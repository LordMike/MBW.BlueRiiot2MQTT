using System.Runtime.Serialization;

namespace MBW.BlueRiiot2MQTT.Features.Enums
{
    internal enum MeasurementStatus
    {
        Unknown,
        
        [EnumMember(Value = "bad")]
        Bad,
        
        [EnumMember(Value = "warning")]
        Warning,
        
        [EnumMember(Value = "ok")]
        Ok
    }
}