using System;
using MBW.BlueRiiot2MQTT.Features;
using MBW.BlueRiiot2MQTT.HASS.Enum;

namespace MBW.BlueRiiot2MQTT.HASS
{
    internal class HassWillContainer
    {
        private readonly HassMqttSensor _sensor;
        
        // "problem" device class: on = problem; off = no problem
        public const string RunningMessage = "OFF";
        public const string NotRunningMessage = "ON";

        public string StateTopic => _sensor.StateTopic;

        public HassWillContainer(SensorStore sensorStore)
        {
            _sensor = sensorStore.Create("BlueRiiot2MQTT Status", "blueriiot2mqtt_status", HassDeviceType.BinarySensor,
                    "blueriiot2mqtt", "status", HassDeviceClass.Problem)
                .SetDeviceProperty(HassMqttSensorDeviceProperty.Name, "BlueRiiot2MQTT")
                .SetDeviceProperty(HassMqttSensorDeviceProperty.SoftwareVersion,  typeof(Program).Assembly.GetName().Version.ToString(3))
                .SetAttribute("started", DateTime.UtcNow)
                .AddDeviceIdentifier("blueriiot2mqtt");
        }
    }
}