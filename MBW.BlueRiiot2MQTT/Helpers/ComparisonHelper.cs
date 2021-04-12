using System;

namespace MBW.BlueRiiot2MQTT.Helpers
{
    internal static class ComparisonHelper
    {
        public static T? GetMax<T>(params T?[] values) where T : struct, IComparable<T>
        {
            T? latest = null;

            foreach (T? value in values)
            {
                if (!value.HasValue)
                    continue;

                if (latest.HasValue && latest.Value.CompareTo(value.Value) > 0)
                    continue;

                latest = value;
            }

            return latest;
        }

        public static T? GetMin<T>(params T?[] values) where T : struct, IComparable<T>
        {
            T? earliest = null;

            foreach (T? value in values)
            {
                if (!value.HasValue)
                    continue;

                if (earliest.HasValue && earliest.Value.CompareTo(value.Value) < 0)
                    continue;

                earliest = value;
            }

            return earliest;
        }
    }
}