using System.Runtime.Serialization;

namespace MBW.BlueRiiot2MQTT.HASS.Enum
{
    internal enum HassMqttSensorDeviceProperty
    {
        None, 

        [EnumMember(Value = "name")]
        Name,

        [EnumMember(Value = "model")]
        Model,

        [EnumMember(Value = "manufacturer")]
        Manufacturer,

        [EnumMember(Value = "sw_version")]
        SoftwareVersion
    }
}