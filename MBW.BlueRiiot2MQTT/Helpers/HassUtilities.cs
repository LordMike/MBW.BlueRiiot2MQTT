using MBW.BlueRiiot2MQTT.HASS;
using MBW.BlueRiiot2MQTT.HASS.Enum;
using MBW.Client.BlueRiiotApi.Objects;

namespace MBW.BlueRiiot2MQTT.Helpers
{
    internal static class HassUtilities
    {
        public static HassMqttSensor SetHassProperties(this HassMqttSensor sensor, SwimmingPool pool)
        {
            return sensor
                .SetDeviceProperty(HassMqttSensorDeviceProperty.Name, pool.Name)
                .AddDeviceIdentifier(pool.SwimmingPoolId);
        }
    }
}