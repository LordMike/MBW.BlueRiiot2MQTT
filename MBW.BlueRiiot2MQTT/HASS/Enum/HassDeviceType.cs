using System.Runtime.Serialization;

namespace MBW.BlueRiiot2MQTT.HASS.Enum
{
    internal enum HassDeviceType
    {
        None,

        [EnumMember(Value = "alarm_control_panel")]
        AlarmControlPanel,

        [EnumMember(Value = "binary_sensor")]
        BinarySensor,

        [EnumMember(Value = "camera")]
        Camera,

        [EnumMember(Value = "cover")]
        Cover,

        [EnumMember(Value = "device_trigger")]
        DeviceTrigger,

        [EnumMember(Value = "fan")]
        Fan,

        [EnumMember(Value = "climate")]
        Climate,

        [EnumMember(Value = "light")]
        Light,

        [EnumMember(Value = "lock")]
        Lock,

        [EnumMember(Value = "sensor")]
        Sensor,

        [EnumMember(Value = "switch")]
        Switch,

        [EnumMember(Value = "vacuum")]
        Vacuum
    }
}