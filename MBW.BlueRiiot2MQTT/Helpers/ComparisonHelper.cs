using System;

namespace MBW.BlueRiiot2MQTT.Helpers
{
    internal static class ComparisonHelper
    {
        public static T? GetMax<T>(params T?[] values) where T : struct, IComparable<T>
        {
            T? latest = null;

            foreach (var value in values)
            {
                if (!value.HasValue)
                    continue;

                if (latest.HasValue && latest.Value.CompareTo(value.Value) > 0)
                    continue;

                latest = value;
            }

            return latest;
        }
    }
}