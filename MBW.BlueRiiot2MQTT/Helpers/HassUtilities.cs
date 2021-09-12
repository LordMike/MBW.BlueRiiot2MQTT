using MBW.Client.BlueRiiotApi.Objects;
using MBW.HassMQTT.DiscoveryModels.Interfaces;
using MBW.HassMQTT.Extensions;
using MBW.HassMQTT.Interfaces;

namespace MBW.BlueRiiot2MQTT.Helpers
{
    internal static class HassUtilities
    {
        public static IDiscoveryDocumentBuilder<TEntity> SetHassPoolProperties<TEntity>(this IDiscoveryDocumentBuilder<TEntity> sensor, SwimmingPool pool) where TEntity : IHassDiscoveryDocument
        {
            return sensor.ConfigureDevice(device =>
            {
                if (!device.Identifiers.Contains(pool.SwimmingPoolId))
                    device.Identifiers.Add(pool.SwimmingPoolId);

                device.Name = pool.Name;
            });
        }

        public static ISensorContainer SetPoolAttributes(this ISensorContainer sensor, SwimmingPool pool)
        {
            return sensor.SetAttribute("pool_id", pool.SwimmingPoolId);
        }
    }
}