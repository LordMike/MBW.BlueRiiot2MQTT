using EnumsNET;
using MBW.BlueRiiot2MQTT.Features.Enums;
using MBW.BlueRiiot2MQTT.HASS;
using MBW.BlueRiiot2MQTT.HASS.Enum;
using MBW.Client.BlueRiiotApi.Objects;

namespace MBW.BlueRiiot2MQTT.Helpers
{
    internal static class MeasurementUtility
    {
        public static MeasurementStatus GetStatus(SwpLastMeasurements measurement)
        {
            if (measurement.OkMin <= measurement.Value && measurement.Value <= measurement.OkMax)
                return MeasurementStatus.Ok;

            if (measurement.WarningLow <= measurement.Value && measurement.Value <= measurement.WarningHigh)
                return MeasurementStatus.Warning;

            return MeasurementStatus.Bad;
        }

        public static void AddAttributes(HassMqttSensor sensor, SwpLastMeasurements measurement)
        {
            sensor.SetAttribute("min", measurement.GaugeMin);
            sensor.SetAttribute("max", measurement.GaugeMax);
            sensor.SetAttribute("okmin", measurement.OkMin);
            sensor.SetAttribute("okmax", measurement.OkMax);
            sensor.SetAttribute("warninglow", measurement.WarningLow);
            sensor.SetAttribute("warninghigh", measurement.WarningHigh);
            sensor.SetAttribute("timestamp", measurement.Timestamp);
            sensor.SetAttribute("source", measurement.Issuer); // Ex. "strip"

            sensor.SetAttribute("states", new[]
            {
                MeasurementStatus.Ok.AsString(EnumFormat.EnumMemberValue),
                MeasurementStatus.Warning.AsString(EnumFormat.EnumMemberValue),
                MeasurementStatus.Bad.AsString(EnumFormat.EnumMemberValue)
            });
        }
    }
}