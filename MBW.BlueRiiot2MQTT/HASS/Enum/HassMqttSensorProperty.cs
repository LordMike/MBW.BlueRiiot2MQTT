using System.Runtime.Serialization;

namespace MBW.BlueRiiot2MQTT.HASS.Enum
{
    internal enum HassMqttSensorProperty
    {
        None, 

        [EnumMember(Value = "unit_of_measurement")]
        UnitOfMeasurement
    }
}