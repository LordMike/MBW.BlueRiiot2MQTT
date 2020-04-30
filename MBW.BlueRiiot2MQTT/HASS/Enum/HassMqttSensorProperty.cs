using System.Runtime.Serialization;

namespace MBW.BlueRiiot2MQTT.HASS.Enum
{
    internal enum HassMqttSensorProperty
    {
        None, 

        [EnumMember(Value = "unit_of_measurement")]
        UnitOfMeasurement,

        [EnumMember(Value = "payload_available")]
        PayloadAvailable,

        [EnumMember(Value = "payload_not_available")]
        PayloadNotAvailable,

        [EnumMember(Value = "payload_on")]
        PayloadOn,

        [EnumMember(Value = "payload_off")]
        PayloadOff
    }
}