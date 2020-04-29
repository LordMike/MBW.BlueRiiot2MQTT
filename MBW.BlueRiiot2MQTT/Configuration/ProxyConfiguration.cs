using System;

namespace MBW.BlueRiiot2MQTT.Configuration
{
    internal  class ProxyConfiguration
    {
        public bool UseProxy { get; set; }

        public Uri ProxyUri { get; set; }
    }
}