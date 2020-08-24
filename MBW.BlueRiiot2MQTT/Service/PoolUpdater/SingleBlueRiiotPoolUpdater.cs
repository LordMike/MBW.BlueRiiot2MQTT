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
    /// Keeps track of a single BlueRiiot pool
    /// </summary>
    internal class SingleBlueRiiotPoolUpdater
    {
        private readonly ILogger _logger;
        private readonly HassMqttManager _hassMqttManager;
        private readonly BlueClient _blueClient;
        private readonly SwimmingPool _pool;
        private readonly FeatureUpdateManager _updateManager;
        private readonly BlueRiiotConfiguration _config;
        private readonly CancellationTokenSource _cancellationToken = new CancellationTokenSource();
        private readonly Task _backgroundTask;
        private readonly AsyncAutoResetEvent _forceSyncResetEvent = new AsyncAutoResetEvent();
        private readonly Random _random = new Random();
        private readonly TimeSpan _minimumInterval = TimeSpan.FromSeconds(30);

        private DateTime _lastMeasurement = DateTime.MinValue;
        private TimeSpan? _measureInterval;

        public SingleBlueRiiotPoolUpdater(ILogger logger, HassMqttManager hassMqttManager, FeatureUpdateManager updateManager, BlueClient blueClient, BlueRiiotConfiguration config, SwimmingPool pool)
        {
            _logger = logger;
            _hassMqttManager = hassMqttManager;
            _pool = pool;
            _updateManager = updateManager;
            _blueClient = blueClient;
            _config = config;

            _backgroundTask = new Task(async () => await Run(), _cancellationToken.Token);
        }

        public void Start()
        {
            _backgroundTask.Start();
        }

        public void Stop()
        {
            _cancellationToken.Cancel();
        }

        public void ForceSync()
        {
            _forceSyncResetEvent.Set();
        }

        private async Task Run()
        {
            ISensorContainer operationalSensor = CreateSystemEntities();

            // Update loop
            DateTime lastRun = DateTime.MinValue;

            while (!_cancellationToken.Token.IsCancellationRequested)
            {
                // Calculate time to next update
                TimeSpan toDelay = CalculateDelay(lastRun);

                // Wait on the force sync reset event, for the specified time.
                // If either the reset event or the time runs out, we do an update
                using (CancellationTokenSource cts = new CancellationTokenSource(toDelay))
                using (CancellationTokenSource linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, _cancellationToken.Token))
                {
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

                _logger.LogDebug("Beginning update for pool {Pool}", _pool.SwimmingPoolId);

                try
                {
                    await PerformUpdate(_cancellationToken.Token);

                    // Track API operational status
                    operationalSensor.SetValue(HassTopicKind.State, BlueRiiotMqttService.OkMessage);
                    operationalSensor.SetAttribute("last_ok", DateTime.UtcNow.ToString("O"));
                }
                catch (OperationCanceledException) when (_cancellationToken.Token.IsCancellationRequested)
                {
                    // Do nothing
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An error occurred while performing the update for pool {Pool}", _pool.SwimmingPoolId);

                    // Track API operational status
                    operationalSensor.SetValue(HassTopicKind.State, BlueRiiotMqttService.ProblemMessage);
                    operationalSensor.SetAttribute("last_bad", DateTime.UtcNow.ToString("O"));
                    operationalSensor.SetAttribute("last_bad_status", e.Message);
                }

                try
                {
                    await _hassMqttManager.FlushAll(_cancellationToken.Token);
                }
                catch (OperationCanceledException) when (_cancellationToken.Token.IsCancellationRequested)
                {
                    // Do nothing
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An error occurred while pushing updated data to MQTT for pool {Pool}", _pool.SwimmingPoolId);
                }

                lastRun = DateTime.UtcNow;
            }
        }

        private ISensorContainer CreateSystemEntities()
        {
            string deviceId = HassUniqueIdBuilder.GetPoolDeviceId(_pool);
            const string entityId = "update_status";

            _hassMqttManager.ConfigureSensor<MqttBinarySensor>(deviceId, entityId)
                   .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
                   .SetHassPoolProperties(_pool)
                   .ConfigureDiscovery(discovery =>
                   {
                       discovery.Name = "Pool update status";
                       discovery.DeviceClass = HassDeviceClass.Problem;

                       discovery.PayloadOn = BlueRiiotMqttService.ProblemMessage;
                       discovery.PayloadOff = BlueRiiotMqttService.OkMessage;
                   })
                   .ConfigureAliveService();

            return _hassMqttManager.GetSensor(deviceId, entityId);
        }

        private TimeSpan CalculateDelay(DateTime lastRun)
        {
            bool isFirstRun = lastRun == DateTime.MinValue;

            TimeSpan toDelay;
            DateTime nextCheck;
            if (_measureInterval.HasValue)
            {
                nextCheck = _lastMeasurement + _measureInterval.Value;
            }
            else
            {
                nextCheck = lastRun + _config.UpdateInterval;
            }

            nextCheck = nextCheck.AddSeconds(_random.Next(0, (int)_config.UpdateIntervalJitter.TotalSeconds));
            toDelay = nextCheck - DateTime.UtcNow;

            // Always wait at least Ns, to avoid error-induced loops
            if (toDelay < _minimumInterval)
                toDelay = isFirstRun ? TimeSpan.FromSeconds(1) : _minimumInterval;

            if (!isFirstRun)
            {
                if (_measureInterval.HasValue)
                    _logger.LogInformation("New data ready for pool '{Pool}' at {DateTime} (interval {Interval}). Waiting {Delay}", _pool.Name, nextCheck, _measureInterval.Value, toDelay);
                else
                    _logger.LogInformation("Delaying till next check on pool '{Pool}', at {DateTime}, waiting {Delay}", _pool.Name, nextCheck, toDelay);
            }

            return toDelay;
        }

        private async Task PerformUpdate(CancellationToken stoppingToken)
        {
            DateTime lastMeasurement = DateTime.MinValue;
            _measureInterval = null;

            List<SwimmingPoolLastMeasurementsGetResponse> measurements = new List<SwimmingPoolLastMeasurementsGetResponse>();

            SwimmingPool pool = _pool;
            _updateManager.Process(pool, pool);

            _logger.LogDebug("Fetching blue devices for {Id} ({Name})", pool.SwimmingPoolId, pool.Name);
            SwimmingPoolBlueDevicesGetResponse blueDevices =
                await _blueClient.GetSwimmingPoolBlueDevices(pool.SwimmingPoolId, stoppingToken);

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