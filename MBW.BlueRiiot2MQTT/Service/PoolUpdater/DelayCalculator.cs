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
                delay = _minimumInterval;

            if (_measurementInterval.HasValue)
                _logger.LogInformation("New data ready for pool '{Pool}' at {DateTime} (interval {Interval}). Waiting {Delay}", _poolName, nextCheck, _measurementInterval.Value, delay);
            else
                _logger.LogInformation("Delaying till next check on pool '{Pool}', at {DateTime}, waiting {Delay}", _poolName, nextCheck, delay);

            return delay;
        }
    }
}