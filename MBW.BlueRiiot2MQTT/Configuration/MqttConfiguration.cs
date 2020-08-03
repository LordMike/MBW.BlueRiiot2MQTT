using System;

namespace MBW.BlueRiiot2MQTT.Configuration
{
    internal class MqttConfiguration
    {
        public string Server { get; set; } = "localhost";

        public int Port { get; set; } = 1883;

        public string Username { get; set; }

        public string Password { get; set; }

        public string ClientId { get; set; } = $"blueriiot2mqtt-{new Random().Next()}";

        public TimeSpan? KeepAlivePeriod { get; set; }
    }
}