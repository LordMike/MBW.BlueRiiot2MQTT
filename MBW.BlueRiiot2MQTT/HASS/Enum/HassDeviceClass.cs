using System.Runtime.Serialization;

namespace MBW.BlueRiiot2MQTT.HASS.Enum
{
    internal enum HassDeviceClass
    {
        /// <summary>
        /// Generic sensor. This is the default and doesn’t need to be set.
        /// </summary>
        None,

        /// <summary>
        /// Percentage of battery that is left.
        /// </summary>
        [EnumMember(Value = "battery")]
        Battery,

        /// <summary>
        /// Percentage of humidity in the air.
        /// </summary>
        [EnumMember(Value = "humidity")]
        Humidity,

        /// <summary>
        /// The current light level in lx or lm.
        /// </summary>
        [EnumMember(Value = "illuminance")]
        Illuminance,

        /// <summary>
        /// Signal strength in dB or dBm.
        /// </summary>
        [EnumMember(Value = "signal_strength")]
        SingalStrength,

        /// <summary>
        /// Temperature in °C or °F.
        /// </summary>
        [EnumMember(Value = "temperature")]
        Temperature,

        /// <summary>
        /// Power in W or kW.
        /// </summary>
        [EnumMember(Value = "power")]
        Power,

        /// <summary>
        /// Pressure in hPa or mbar.
        /// </summary>
        [EnumMember(Value = "pressure")]
        Pressure,

        /// <summary>
        /// Datetime object or timestamp string.
        /// </summary>
        [EnumMember(Value = "timestamp")]
        Timestamp,

        /// <summary>
        /// BinarySensor: on means problem detected, off means no problem (OK)
        /// </summary>
        [EnumMember(Value = "problem")]
        Problem
    }
}