using System.Linq;
using MBW.BlueRiiot2MQTT.HASS;
using MBW.BlueRiiot2MQTT.HASS.Enum;
using MBW.BlueRiiot2MQTT.Helpers;
using MBW.Client.BlueRiiotApi.Objects;
using MBW.Client.BlueRiiotApi.RequestsResponses;

namespace MBW.BlueRiiot2MQTT.Features.Pool
{
    internal class PoolLastUpdateFeature : FeatureUpdaterBaseTyped<SwimmingPoolLastMeasurementsGetResponse>
    {
        public PoolLastUpdateFeature(SensorStore sensorStore) : base(sensorStore)
        {
        }

        protected override string GetUniqueId(SwimmingPool pool, SwimmingPoolLastMeasurementsGetResponse obj)
        {
            return $"pool_{pool.SwimmingPoolId}_last_measurement";
        }

        protected override void CreateSensor(SwimmingPool pool, string uniqueId, SwimmingPoolLastMeasurementsGetResponse obj)
        {
            SensorStore.Create($"{pool.Name} Last Measurement", uniqueId, HassDeviceType.Sensor, $"pool_{pool.SwimmingPoolId}", "last_measurement", HassDeviceClass.Timestamp)
                .SetHassProperties(pool);
        }

        protected override void UpdateInternal(SwimmingPool pool, string uniqueId, SwimmingPoolLastMeasurementsGetResponse obj)
        {
            HassMqttSensor sensor = SensorStore.Get(uniqueId);

            SwpLastMeasurements lastMeasurement = null;
            if (obj.LastBlueMeasureTimestamp.HasValue &&
                (!obj.LastStripTimestamp.HasValue || obj.LastBlueMeasureTimestamp > obj.LastStripTimestamp))
            {
                // Blue measurement is latest
                lastMeasurement = obj.Data.OrderByDescending(s => s.Timestamp).FirstOrDefault();

                sensor.SetValue(obj.LastBlueMeasureTimestamp);
                sensor.SetAttribute("method", "blue");
            }
            else if (obj.LastStripTimestamp.HasValue)
            {
                // Strip measurement is latest
                lastMeasurement = obj.Data.OrderByDescending(s => s.Timestamp).FirstOrDefault();

                sensor.SetValue(obj.LastStripTimestamp);
                sensor.SetAttribute("method", "strip");
            }
            else
            {
                // No measurements
                sensor.SetValue(null);
            }

            if (lastMeasurement != null)
                sensor.SetAttribute("measurement", lastMeasurement.Name);
        }
    }
}