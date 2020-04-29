using System.Runtime.Serialization;

namespace MBW.BlueRiiot2MQTT.HASS.Enum
{
    internal enum MeasurementStatus
    {
        Unknown,
        
        [EnumMember(Value = "fatal")]
        Fatal,
        
        [EnumMember(Value = "warning")]
        Warning,
        
        [EnumMember(Value = "ok")]
        Ok
    }
}