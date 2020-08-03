using System;

namespace MBW.BlueRiiot2MQTT.Helpers
{
    internal static class ComparisonHelper
    {
        public static DateTime? GetMax(DateTime? a, DateTime? b)
        {
            if (!a.HasValue)
                return b;

            if (!b.HasValue)
                return a;

            return a.Value > b.Value ? a : b;
        }
    }
}