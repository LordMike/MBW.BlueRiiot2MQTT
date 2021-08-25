using System;

namespace MBW.BlueRiiot2MQTT.Service.PoolUpdater
{
    internal class DelayCalculatorConfig
    {
        public TimeSpan UpdateInterval { get; set; }
        public TimeSpan? UpdateIntervalWhenAllDevicesAsleep { get; set; }
        public TimeSpan UpdateIntervalJitter { get; set; }
        public TimeSpan MaxBackoffInterval { get; set; }
    }
}