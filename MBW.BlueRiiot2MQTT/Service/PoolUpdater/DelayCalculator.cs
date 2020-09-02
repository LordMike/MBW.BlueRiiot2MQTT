using System;
using MBW.BlueRiiot2MQTT.Configuration;
using Microsoft.Extensions.Logging;

namespace MBW.BlueRiiot2MQTT.Service.PoolUpdater
{
    internal class DelayCalculator
    {
        private readonly TimeSpan _minimumInterval = TimeSpan.FromMinutes(1);
        private readonly Random _random = new Random();
        private readonly ILogger _logger;
        private readonly BlueRiiotConfiguration _config;
        private readonly string _poolName;

        private TimeSpan? _measurementInterval;
        private DateTime? _lastAutoMeasurement;
        private int _minimumIntervalUsedCounter;

        public DelayCalculator(ILogger logger, BlueRiiotConfiguration config, string poolName)
        {
            _logger = logger;
            _config = config;
            _poolName = poolName;
        }

        public void TrackMeasureInterval(TimeSpan? interval)
        {
            _measurementInterval = interval;
        }

        public void TrackLastAutoMeasurement(DateTime lastAutoMeasurement)
        {
            if (lastAutoMeasurement == DateTime.MinValue)
                _lastAutoMeasurement = null;
            else
                _lastAutoMeasurement = lastAutoMeasurement;
        }

        public TimeSpan CalculateNextRun(DateTime? lastRun)
        {
            if (!lastRun.HasValue)
            {
                // First runs are a special case
                return TimeSpan.FromMilliseconds(1);
            }

            DateTime nextCheck;
            if (_measurementInterval.HasValue && _lastAutoMeasurement.HasValue)
            {
                nextCheck = _lastAutoMeasurement.Value + _measurementInterval.Value;
            }
            else
            {
                nextCheck = lastRun.Value + _config.UpdateInterval;
            }

            // Add random jitter
            nextCheck = nextCheck.AddSeconds(_random.Next(0, (int)_config.UpdateIntervalJitter.TotalSeconds));
            TimeSpan delay = nextCheck - DateTime.UtcNow;

            // We must wait at least minimumInterval between each run, to avoid error-induced loops
            if (delay <= _minimumInterval)
            {
                // In some cases, we won't get updates, which means the last measurement will always be old, and next check will be "now"
                // In these cases, do an incremental back off
                _minimumIntervalUsedCounter++;

                // No-updates back off should be minimumInterval * updates^2
                // 3 updates = 00:01 * 3^2 = 00:09
                // 8 updates = 00:01 * 8^2 = 01:04
                // .. up to a max interval

                // Note: We ensure our math is always stable
                int cappedCounter = Math.Clamp(_minimumIntervalUsedCounter, 1, 30);
                TimeSpan backoffDelay = _minimumInterval * (int)Math.Pow(cappedCounter, 2);

                if (backoffDelay >  _config.MaxBackoffInterval)
                    backoffDelay =  _config.MaxBackoffInterval;

                nextCheck = DateTime.UtcNow + backoffDelay;
                delay = nextCheck - DateTime.UtcNow;

                _logger.LogWarning("There were {Count} consecutive updates without new data for '{Pool}', setting next run to be {DateTime}, waiting {Delay}", _minimumIntervalUsedCounter, _poolName, nextCheck, backoffDelay);
            }
            else
            {
                _minimumIntervalUsedCounter = 0;

                if (_measurementInterval.HasValue)
                    _logger.LogInformation("New data ready for pool '{Pool}' at {DateTime} (interval {Interval}). Waiting {Delay}", _poolName, nextCheck, _measurementInterval.Value, delay);
                else
                    _logger.LogInformation("Delaying till next check on pool '{Pool}', at {DateTime}, waiting {Delay}", _poolName, nextCheck, delay);
            }

            return delay;
        }
    }
}