using MBW.Client.BlueRiiotApi.Objects;
using MBW.HassMQTT.DiscoveryModels;
using MBW.HassMQTT.Extensions;
using MBW.HassMQTT.Interfaces;

namespace MBW.BlueRiiot2MQTT.Helpers
{
    internal static class HassUtilities
    {
        public static IDiscoveryDocumentBuilder<TEntity> SetHassPoolProperties<TEntity>(this IDiscoveryDocumentBuilder<TEntity> sensor, SwimmingPool pool) where TEntity : MqttSensorDiscoveryBase
        {
            return sensor.ConfigureDevice(device =>
            {
                device.Name = pool.Name;
                device.Identifiers = new[] {pool.SwimmingPoolId};
            });
        }

        public static ISensorContainer SetPoolAttributes(this ISensorContainer sensor, SwimmingPool pool)
        {
            return sensor.SetAttribute("pool_id", pool.SwimmingPoolId);
        }
    }
}