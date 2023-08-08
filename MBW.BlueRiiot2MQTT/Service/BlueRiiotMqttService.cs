using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MBW.BlueRiiot2MQTT.Configuration;
using MBW.BlueRiiot2MQTT.HASS;
using MBW.BlueRiiot2MQTT.Service.PoolUpdater;
using MBW.Client.BlueRiiotApi;
using MBW.Client.BlueRiiotApi.Objects;
using MBW.Client.BlueRiiotApi.RequestsResponses;
using MBW.HassMQTT;
using MBW.HassMQTT.CommonServices.AliveAndWill;
using MBW.HassMQTT.DiscoveryModels.Enum;
using MBW.HassMQTT.DiscoveryModels.Models;
using MBW.HassMQTT.Extensions;
using MBW.HassMQTT.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MBW.BlueRiiot2MQTT.Service
{
    internal class BlueRiiotMqttService : BackgroundService
    {
        private readonly ILogger<BlueRiiotMqttService> _logger;
        private readonly BlueClient _blueClient;
        private readonly HassMqttManager _hassMqttManager;
        private readonly SingleBlueRiiotPoolUpdaterFactory _updaterFactory;
        private readonly BlueRiiotConfiguration _config;
        private readonly ConcurrentDictionary<string, List<IBackgroundUpdater>> _updaters = new ConcurrentDictionary<string, List<IBackgroundUpdater>>(StringComparer.Ordinal);

        public const string OkMessage = "ok";
        public const string ProblemMessage = "problem";

        public BlueRiiotMqttService(
            ILogger<BlueRiiotMqttService> logger,
            IOptions<BlueRiiotConfiguration> config,
            BlueClient blueClient,
            HassMqttManager hassMqttManager,
            SingleBlueRiiotPoolUpdaterFactory updaterFactory)
        {
            _logger = logger;
            _blueClient = blueClient;
            _hassMqttManager = hassMqttManager;
            _updaterFactory = updaterFactory;
            _config = config.Value;
        }

        public void ForceSync()
        {
            _logger.LogInformation("Forcing a sync for all known pools");

            foreach (IBackgroundUpdater updater in _updaters.Values.SelectMany(s => s))
                updater.ForceSync();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ISensorContainer operationalSensor = CreateSystemEntities();

            try
            {
                // Update loop
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogDebug("Beginning update");

                    try
                    {
                        await PerformUpdate(stoppingToken);

                        // Track API operational status
                        operationalSensor.SetValue(HassTopicKind.State, OkMessage);
                        operationalSensor.SetAttribute("last_ok", DateTime.UtcNow.ToString("O"));
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        // Do nothing
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "An error occurred while performing the update");

                        // Track API operational status
                        operationalSensor.SetValue(HassTopicKind.State, ProblemMessage);
                        operationalSensor.SetAttribute("last_bad", DateTime.UtcNow.ToString("O"));
                        operationalSensor.SetAttribute("last_bad_status", e.Message);
                    }

                    await Task.Delay(_config.DiscoveryInterval, stoppingToken);
                }
            }
            finally
            {
                // Stop all updaters
                _logger.LogInformation("Stopping all pool updaters");

                foreach (IBackgroundUpdater updater in _updaters.Values.SelectMany(s => s))
                    updater.Stop();
            }
        }

        private ISensorContainer CreateSystemEntities()
        {
            _hassMqttManager.ConfigureSensor<MqttBinarySensor>(HassUniqueIdBuilder.GetSystemDeviceId(), "api_operational")
                .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
                .ConfigureDevice(device =>
                {
                    if (!device.Identifiers.Contains(HassUniqueIdBuilder.GetSystemDeviceId()))
                        device.Identifiers.Add(HassUniqueIdBuilder.GetSystemDeviceId());

                    device.Name = "BlueRiiot2MQTT";
                    device.SwVersion = typeof(Program).Assembly.GetName().Version.ToString(3);
                })
                .ConfigureDiscovery(discovery =>
                {
                    discovery.Name = "API Operational";
                    discovery.DeviceClass = HassBinarySensorDeviceClass.Problem;

                    discovery.PayloadOn = ProblemMessage;
                    discovery.PayloadOff = OkMessage;
                })
                .ConfigureAliveService();

            return _hassMqttManager.GetSensor(HassUniqueIdBuilder.GetSystemDeviceId(), "api_operational");
        }

        private async Task PerformUpdate(CancellationToken stoppingToken)
        {
            SwimmingPoolGetResponse pools = await _blueClient.GetSwimmingPools(token: stoppingToken);

            // Start any previously unknown pools
            HashSet<string> expectedPools = _updaters.Keys.ToHashSet(StringComparer.Ordinal);

            foreach (UserSwimmingPool pool in pools.Data)
            {
                if (pool.SwimmingPool == null)
                {
                    _logger.LogWarning("Identified an incomplete pool, {name}, maybe it's new and not ready yet", pool.Name);
                    continue;
                }

                expectedPools.Remove(pool.SwimmingPoolId);

                if (_updaters.ContainsKey(pool.SwimmingPoolId))
                {
                    _logger.LogTrace("Identified known pool, {Pool}", pool.SwimmingPoolId);
                    continue;
                }

                // New pool!
                _logger.LogInformation("Discovered new pool, '{Name}' ({Pool})", pool.Name, pool.SwimmingPoolId);

                SingleBlueRiiotPoolUpdater newUpdater = _updaterFactory.CreateUpdater(pool.SwimmingPool);
                newUpdater.Start();

                SingleBlueRiiotPoolWeatherUpdater weatherUpdater = _updaterFactory.CreateWeatherUpdater(pool.SwimmingPool);
                weatherUpdater.Start();

                _updaters[pool.SwimmingPoolId] = new List<IBackgroundUpdater>
                {
                    newUpdater,
                    weatherUpdater
                };
            }

            // Remove any leftovers
            foreach (string poolId in expectedPools)
            {
                _logger.LogWarning("Removing pool that no longer exists, {Pool}", poolId);

                if (_updaters.Remove(poolId, out List<IBackgroundUpdater> updaters))
                {
                    foreach (IBackgroundUpdater updater in updaters)
                        updater.Stop();
                }
            }
        }
    }
}