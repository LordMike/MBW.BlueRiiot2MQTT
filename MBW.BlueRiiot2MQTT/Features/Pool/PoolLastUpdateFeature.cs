using System.Collections.Generic;
using System.Linq;
using MBW.BlueRiiot2MQTT.Features.Pool.Bases;
using MBW.BlueRiiot2MQTT.HASS;
using MBW.BlueRiiot2MQTT.HASS.Enum;
using MBW.BlueRiiot2MQTT.Helpers;
using MBW.Client.BlueRiiotApi.Objects;
using MBW.Client.BlueRiiotApi.RequestsResponses;

namespace MBW.BlueRiiot2MQTT.Features.Pool
{
    internal class PoolLastUpdateFeature : LastMeasurementsFeatureBase
    {
        public PoolLastUpdateFeature(SensorStore sensorStore) : base(sensorStore)
        {
        }

        protected override string GetUniqueId(SwimmingPool pool, List<SwimmingPoolLastMeasurementsGetResponse> measurements, SwimmingPoolLastMeasurementsGetResponse latest)
        {
            return $"pool_{pool.SwimmingPoolId}_last_measurement";
        }

        protected override void CreateSensor(SwimmingPool pool, string uniqueId,  List<SwimmingPoolLastMeasurementsGetResponse> measurements, SwimmingPoolLastMeasurementsGetResponse latest)
        {
            SensorStore.Create($"{pool.Name} Last Measurement", uniqueId, HassDeviceType.Sensor, $"pool_{pool.SwimmingPoolId}", "last_measurement", HassDeviceClass.Timestamp)
                .SetHassProperties(pool);
        }
        
        protected override void UpdateInternal(SwimmingPool pool, string uniqueId, List<SwimmingPoolLastMeasurementsGetResponse> measurements, SwimmingPoolLastMeasurementsGetResponse latest)
        {
            HassMqttSensor sensor = SensorStore.Get(uniqueId);

            if (latest == null)
            {
                // No measurements
                sensor.SetAttribute("measurement", "none");
                sensor.SetValue(null);

                return;
            }

            SwpLastMeasurements lastMeasurement = null;
            if (latest.LastBlueMeasureTimestamp.HasValue &&
                (!latest.LastStripTimestamp.HasValue || latest.LastBlueMeasureTimestamp > latest.LastStripTimestamp))
            {
                // Blue measurement is latest
                lastMeasurement = latest.Data.OrderByDescending(s => s.Timestamp).FirstOrDefault();

                sensor.SetValue(latest.LastBlueMeasureTimestamp);
                sensor.SetAttribute("method", "blue");
            }
            else if (latest.LastStripTimestamp.HasValue)
            {
                // Strip measurement is latest
                lastMeasurement = latest.Data.OrderByDescending(s => s.Timestamp).FirstOrDefault();

                sensor.SetValue(latest.LastStripTimestamp);
                sensor.SetAttribute("method", "strip");
            }
            else
            {
                // No measurements
                sensor.SetAttribute("measurement", "none");
                sensor.SetValue(null);
            }

            if (lastMeasurement != null)
                sensor.SetAttribute("measurement", lastMeasurement.Name);
        }
    }
}