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
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace MBW.BlueRiiot2MQTT.Service.PoolUpdater
{
    /// <summary>
    /// This updates Weather stuff only
    /// The weather API has been dodgy in the past
    /// https://github.com/LordMike/MBW.BlueRiiot2MQTT/issues/48
    /// </summary>
    internal class SingleBlueRiiotPoolWeatherUpdater : IBackgroundUpdater
    {
        private readonly ILogger _logger;
        private readonly HassMqttManager _hassMqttManager;
        private readonly BlueClient _blueClient;
        private readonly SwimmingPool _pool;
        private readonly FeatureUpdateManager _updateManager;
        private readonly BlueRiiotConfiguration _config;
        private readonly CancellationTokenSource _stoppingToken = new CancellationTokenSource();
        private readonly Task _backgroundTask;
        private readonly AsyncAutoResetEvent _forceSyncResetEvent = new AsyncAutoResetEvent();
        private readonly DelayCalculator _delayCalculator;

        private DateTime _lastMeasurement = DateTime.MinValue;

        public SingleBlueRiiotPoolWeatherUpdater(ILogger logger, HassMqttManager hassMqttManager, FeatureUpdateManager updateManager, BlueClient blueClient, BlueRiiotConfiguration config, SwimmingPool pool)
        {
            _logger = logger;
            _hassMqttManager = hassMqttManager;
            _pool = pool;
            _updateManager = updateManager;
            _blueClient = blueClient;
            _config = config;

            DelayCalculatorConfig delayCalcConfig = new DelayCalculatorConfig
            {
                UpdateInterval = config.WeatherUpdateInterval,
                UpdateIntervalWhenAllDevicesAsleep = config.WeatherUpdateIntervalWhenAllDevicesAsleep,
                UpdateIntervalJitter = config.UpdateIntervalJitter,
                MaxBackoffInterval = config.MaxBackoffInterval
            };
            _delayCalculator = new DelayCalculator(_logger, delayCalcConfig, pool.Name);

            _backgroundTask = new Task(async () => await Run(), _stoppingToken.Token);
        }

        public void Start()
        {
            _backgroundTask.Start();
        }

        public void Stop()
        {
            _stoppingToken.Cancel();
        }

        public void ForceSync()
        {
            _forceSyncResetEvent.Set();
        }

        private async Task Run()
        {
            ISensorContainer operationalSensor = CreateSystemEntities();

            using IDisposable _ = _logger.BeginScope(new Dictionary<string, object>
            {
                { "PoolId", _pool.SwimmingPoolId }
            });

            // Update loop
            while (!_stoppingToken.Token.IsCancellationRequested)
            {
                _logger.LogDebug("Beginning update for pool {Pool}", _pool.SwimmingPoolId);

                try
                {
                    await PerformUpdate(_stoppingToken.Token);

                    // Track API operational status
                    operationalSensor.SetValue(HassTopicKind.State, BlueRiiotMqttService.OkMessage);
                    operationalSensor.SetAttribute("last_ok", DateTime.UtcNow);
                }
                catch (OperationCanceledException) when (_stoppingToken.Token.IsCancellationRequested)
                {
                    // Do nothing
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An error occurred while performing the update for pool {Pool}", _pool.SwimmingPoolId);

                    // Track API operational status
                    operationalSensor.SetValue(HassTopicKind.State, BlueRiiotMqttService.ProblemMessage);
                    operationalSensor.SetAttribute("last_bad", DateTime.UtcNow);
                    operationalSensor.SetAttribute("last_bad_status", e.Message);
                }

                TimeSpan? runDelay;
                if (_config.EnableSchedule)
                {
                    // Calculate time to next update
                    runDelay = _delayCalculator.CalculateNextRun(DateTime.UtcNow);
                    DateTime runNext = DateTime.UtcNow + runDelay.Value;

                    operationalSensor.SetAttribute("next_run", runNext);
                }
                else
                {
                    // We will forever wait for manual sync
                    runDelay = null;

                    operationalSensor.SetAttribute("next_run", "manual");
                }

                try
                {
                    await _hassMqttManager.FlushAll(_stoppingToken.Token);
                }
                catch (OperationCanceledException) when (_stoppingToken.Token.IsCancellationRequested)
                {
                    // Do nothing
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An error occurred while pushing updated data to MQTT for pool {Pool}", _pool.SwimmingPoolId);
                }

                // Wait on the force sync reset event, for the specified time.
                // If either the reset event or the time runs out, we do an update
                using (CancellationTokenSource cts = new CancellationTokenSource())
                using (CancellationTokenSource linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, _stoppingToken.Token))
                {
                    if (runDelay.HasValue)
                        cts.CancelAfter(runDelay.Value);

                    try
                    {
                        await _forceSyncResetEvent.WaitAsync(linkedToken.Token);

                        // We were forced
                        _logger.LogDebug("Forcing a sync for pool {Pool} with BlueRiiot", _pool.SwimmingPoolId);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }
            }
        }

        private ISensorContainer CreateSystemEntities()
        {
            string deviceId = HassUniqueIdBuilder.GetPoolDeviceId(_pool);
            const string entityId = "weather_update_status";

            _hassMqttManager.ConfigureSensor<MqttBinarySensor>(deviceId, entityId)
                .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
                .SetHassPoolProperties(_pool)
                .ConfigureDiscovery(discovery =>
                {
                    discovery.Name = "Pool weather update status";
                    discovery.DeviceClass = HassBinarySensorDeviceClass.Problem;

                    discovery.PayloadOn = BlueRiiotMqttService.ProblemMessage;
                    discovery.PayloadOff = BlueRiiotMqttService.OkMessage;
                })
                .ConfigureAliveService();

            return _hassMqttManager.GetSensor(deviceId, entityId);
        }

        private async Task PerformUpdate(CancellationToken stoppingToken)
        {
            DateTime lastMeasurement = DateTime.MinValue;

            _logger.LogDebug("Fetching weather for {Id} ({Name})", _pool.SwimmingPoolId, _pool.Name);

            SwimmingPoolWeatherGetResponse weather = await _blueClient.GetSwimmingPoolWeather(_pool.SwimmingPoolId, _config.Language, stoppingToken);
            _updateManager.Process(_pool, weather.Data);

            _logger.LogDebug("Fetching blue devices for {Id} ({Name})", _pool.SwimmingPoolId, _pool.Name);
            SwimmingPoolBlueDevicesGetResponse blueDevices = await _blueClient.GetSwimmingPoolBlueDevices(_pool.SwimmingPoolId, stoppingToken);

            bool anyBlueDeviceIsAwake = false;
            foreach (SwimmingPoolDevice blueDevice in blueDevices.Data)
            {
                // Track sleep states
                anyBlueDeviceIsAwake |= blueDevice.BlueDevice.SleepState == "awake";
            }

            _delayCalculator.TrackLastAutoMeasurement(DateTime.UtcNow);
            _delayCalculator.TrackSleepState(anyBlueDeviceIsAwake);

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