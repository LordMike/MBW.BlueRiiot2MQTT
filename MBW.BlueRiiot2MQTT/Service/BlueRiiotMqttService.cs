using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MBW.BlueRiiot2MQTT.Configuration;
using MBW.BlueRiiot2MQTT.Features;
using MBW.BlueRiiot2MQTT.HASS;
using MBW.BlueRiiot2MQTT.Helpers;
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
        private readonly FeatureUpdateManager _updateManager;
        private readonly HassMqttManager _hassMqttManager;
        private readonly BlueRiiotConfiguration _config;
        private readonly Random _random = new Random();
        private readonly TimeSpan _minimumInterval = TimeSpan.FromSeconds(30);

        public const string OkMessage = "ok";
        public const string ProblemMessage = "problem";

        private DateTime _lastMeasurement = DateTime.MinValue;
        private TimeSpan? _measureInterval;

        public BlueRiiotMqttService(
            ILogger<BlueRiiotMqttService> logger,
            IOptions<BlueRiiotConfiguration> config,
            BlueClient blueClient,
            FeatureUpdateManager updateManager,
            HassMqttManager hassMqttManager)
        {
            _logger = logger;
            _blueClient = blueClient;
            _updateManager = updateManager;
            _hassMqttManager = hassMqttManager;
            _config = config.Value;
        }

        private TimeSpan CalculateDelay(DateTime lastRun)
        {
            TimeSpan toDelay;
            DateTime nextCheck;
            if (_measureInterval.HasValue)
            {
                nextCheck = (_lastMeasurement + _measureInterval.Value)
                    .AddSeconds(_random.Next(0, (int)_config.UpdateIntervalJitter.TotalSeconds));
            }
            else
            {
                nextCheck = (lastRun + _config.UpdateInterval)
                    .AddSeconds(_random.Next(0, (int)_config.UpdateIntervalJitter.TotalSeconds));
            }

            toDelay = nextCheck - DateTime.UtcNow;

            // Always wait at least Ns, to avoid error-induced loops
            if (toDelay < _minimumInterval)
            {
                if (lastRun == DateTime.MinValue)
                    // First delay should be minimal
                    toDelay = TimeSpan.FromSeconds(1);
                else
                    toDelay = _minimumInterval;
            }

            if (_measureInterval.HasValue)
                _logger.LogInformation("New data ready at {DateTime} (interval {Interval}). Waiting {Delay}", nextCheck, _measureInterval.Value, toDelay);
            else
                _logger.LogInformation("Delaying till next check, at {DateTime}, waiting {Delay}", nextCheck, toDelay);

            return toDelay;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            CreateSystemEntities();

            ISensorContainer operationalSensor = _hassMqttManager.GetSensor(HassUniqueIdBuilder.GetSystemDeviceId(), "api_operational");

            // Update loop
            DateTime lastRun = DateTime.MinValue;

            while (!stoppingToken.IsCancellationRequested)
            {
                // Calculate time to next update
                TimeSpan toDelay = CalculateDelay(lastRun);

                await Task.Delay(toDelay, stoppingToken);

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

                try
                {
                    await _hassMqttManager.FlushAll(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Do nothing
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An error occurred while pushing updated data to MQTT");
                }

                lastRun = DateTime.UtcNow;
            }
        }

        private void CreateSystemEntities()
        {
            _hassMqttManager.ConfigureSensor<MqttBinarySensor>(HassUniqueIdBuilder.GetSystemDeviceId(), "api_operational")
                .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
                .ConfigureDevice(device =>
                {
                    device.Name = "BlueRiiot2MQTT";
                    device.Identifiers = new[] { HassUniqueIdBuilder.GetSystemDeviceId() };
                    device.SwVersion = typeof(Program).Assembly.GetName().Version.ToString(3);
                })
                .ConfigureDiscovery(discovery =>
                {
                    discovery.Name = "BlueRiiot2MQTT API Operational";
                    discovery.DeviceClass = HassDeviceClass.Problem;

                    discovery.PayloadOn = ProblemMessage;
                    discovery.PayloadOff = OkMessage;
                })
                .ConfigureAliveService();
        }

        private async Task PerformUpdate(CancellationToken stoppingToken)
        {
            SwimmingPoolGetResponse pools = await _blueClient.GetSwimmingPools(token: stoppingToken);
            DateTime lastMeasurement = DateTime.MinValue;
            _measureInterval = null;

            foreach (UserSwimmingPool userPool in pools.Data)
            {
                if (userPool.SwimmingPool == null)
                {
                    _logger.LogWarning("Identified an incomplete pool, {name}, maybe it's new and not ready yet", userPool.Name);
                    continue;
                }

                List<SwimmingPoolLastMeasurementsGetResponse> measurements = new List<SwimmingPoolLastMeasurementsGetResponse>();

                SwimmingPool pool = userPool.SwimmingPool;
                _updateManager.Process(pool, pool);

                _logger.LogDebug("Fetching blue devices for {Id} ({Name})", pool.SwimmingPoolId, pool.Name);
                SwimmingPoolBlueDevicesGetResponse blueDevices = await _blueClient.GetSwimmingPoolBlueDevices(pool.SwimmingPoolId, stoppingToken);

                foreach (SwimmingPoolDevice blueDevice in blueDevices.Data)
                {
                    _logger.LogDebug("Fetching measurements for {Id}, blue {Serial} ({Name})", pool.SwimmingPoolId, blueDevice.BlueDeviceSerial, pool.Name);
                    _updateManager.Process(pool, blueDevice);

                    SwimmingPoolLastMeasurementsGetResponse blueMeasurement = await _blueClient.GetBlueLastMeasurements(pool.SwimmingPoolId, blueDevice.BlueDeviceSerial, token: stoppingToken);

                    if (blueDevice.BlueDevice.WakePeriod > 0)
                        _measureInterval = ComparisonHelper.GetMin(_measureInterval, TimeSpan.FromSeconds(blueDevice.BlueDevice.WakePeriod));

                    measurements.Add(blueMeasurement);
                    lastMeasurement = ComparisonHelper.GetMax(lastMeasurement, blueMeasurement.LastStripTimestamp, blueMeasurement.LastBlueMeasureTimestamp).GetValueOrDefault();
                }

                _logger.LogDebug("Fetching guidance for {Id} ({Name})", pool.SwimmingPoolId, pool.Name);

                SwimmingPoolGuidanceGetResponse guidance = await _blueClient.GetSwimmingPoolGuidance(pool.SwimmingPoolId, _config.Language, token: stoppingToken);
                _updateManager.Process(pool, guidance);

                _logger.LogDebug("Fetching measurements for {Id} ({Name})", pool.SwimmingPoolId, pool.Name);

                SwimmingPoolLastMeasurementsGetResponse measurement = await _blueClient.GetSwimmingPoolLastMeasurements(pool.SwimmingPoolId, stoppingToken);
                measurements.Add(measurement);
                lastMeasurement = ComparisonHelper.GetMax(lastMeasurement, measurement.LastStripTimestamp, lastMeasurement, measurement.LastBlueMeasureTimestamp).GetValueOrDefault();

                _updateManager.Process(pool, measurements);

                _logger.LogDebug("Fetching weather for {Id} ({Name})", pool.SwimmingPoolId, pool.Name);

                SwimmingPoolWeatherGetResponse weather = await _blueClient.GetSwimmingPoolWeather(pool.SwimmingPoolId, _config.Language, stoppingToken);
                _updateManager.Process(pool, weather.Data);

                //_logger.LogDebug("Fetching status for {Id} ({Name})", pool.SwimmingPoolId, pool.Name);
                //SwimmingPoolStatusGetResponse status = await _blueClient.GetSwimmingPoolStatus(pool.SwimmingPoolId, stoppingToken);
                //_updateManager.Update(pool, status);
            }

            if (lastMeasurement > _lastMeasurement)
            {
                _lastMeasurement = lastMeasurement;

                if (_config.ReportUnchangedValues)
                {
                    // A new measurement was made, report potentially unchanged values
                    _hassMqttManager.MarkAllValuesDirty();
                }
            }
        }
    }
}