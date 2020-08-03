using MBW.Client.BlueRiiotApi.Objects;

namespace MBW.BlueRiiot2MQTT.HASS
{
    internal static class HassUniqueIdBuilder
    {
        public static string GetSystemDeviceId()
        {
            return "BlueRiiot2MQTT";
        }

        public static string GetPoolDeviceId(SwimmingPool pool)
        {
            return "pool_" + pool.SwimmingPoolId;
        }

        public static string GetPoolDeviceId(UserSwimmingPool pool)
        {
            return "pool_" + pool.SwimmingPoolId;
        }
    }
}