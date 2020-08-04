using System;

namespace MBW.BlueRiiot2MQTT.Configuration
{
    internal class BlueRiiotConfiguration
    {
        public string Username { get; set; }

        public string Password { get; set; }

        public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromHours(1);

        public TimeSpan UpdateIntervalJitter { get; set; } = TimeSpan.FromMinutes(2);

        public string Language { get; set; }

        public bool ReportUnchangedValues { get; set; } = false;
    }
}