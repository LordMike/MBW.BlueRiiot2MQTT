using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MBW.BlueRiiot2MQTT.Features.Pool.Bases;
using MBW.BlueRiiot2MQTT.HASS;
using MBW.BlueRiiot2MQTT.Helpers;
using MBW.Client.BlueRiiotApi.Objects;
using MBW.Client.BlueRiiotApi.RequestsResponses;
using MBW.HassMQTT;
using MBW.HassMQTT.CommonServices.AliveAndWill;
using MBW.HassMQTT.DiscoveryModels.Enum;
using MBW.HassMQTT.DiscoveryModels.Models;
using MBW.HassMQTT.Extensions;
using MBW.HassMQTT.Interfaces;

namespace MBW.BlueRiiot2MQTT.Features.Pool
{
    [UsedImplicitly]
    internal class PoolLastUpdateFeature : LastMeasurementsFeatureBase
    {
        const string AttributeMeasurement = "measurement";

        public PoolLastUpdateFeature(HassMqttManager hassMqttManager) : base(hassMqttManager)
        {
        }

        protected override void CreateSensor(SwimmingPool pool, List<SwimmingPoolLastMeasurementsGetResponse> measurements, SwimmingPoolLastMeasurementsGetResponse latest)
        {
            HassMqttManager.ConfigureSensor<MqttSensor>(HassUniqueIdBuilder.GetPoolDeviceId(pool), "last_measurement")
                .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
                .SetHassProperties(pool)
                .ConfigureDiscovery(discovery =>
                {
                    discovery.Name = $"{pool.Name} Last Measurement";
                    discovery.DeviceClass = HassDeviceClass.Timestamp;
                })
                .ConfigureAliveService();
        }

        protected override void UpdateInternal(SwimmingPool pool, List<SwimmingPoolLastMeasurementsGetResponse> measurements, SwimmingPoolLastMeasurementsGetResponse latest)
        {
            ISensorContainer sensor = HassMqttManager.GetSensor(HassUniqueIdBuilder.GetPoolDeviceId(pool), "last_measurement");

            if (latest == null)
            {
                // No measurements
                sensor.SetValue(HassTopicKind.State, null);
                sensor.SetAttribute(AttributeMeasurement, "none");

                return;
            }

            SwpLastMeasurements lastMeasurement = null;
            if (latest.LastBlueMeasureTimestamp.HasValue &&
                (!latest.LastStripTimestamp.HasValue || latest.LastBlueMeasureTimestamp > latest.LastStripTimestamp))
            {
                // Blue measurement is latest
                lastMeasurement = latest.Data.OrderByDescending(s => s.Timestamp).FirstOrDefault();

                sensor.SetValue(HassTopicKind.State, latest.LastBlueMeasureTimestamp);
                sensor.SetAttribute("method", "blue");
            }
            else if (latest.LastStripTimestamp.HasValue)
            {
                // Strip measurement is latest
                lastMeasurement = latest.Data.OrderByDescending(s => s.Timestamp).FirstOrDefault();

                sensor.SetValue(HassTopicKind.State, latest.LastStripTimestamp);
                sensor.SetAttribute("method", "strip");
            }
            else
            {
                // No measurements
                sensor.SetAttribute(AttributeMeasurement, "none");
                sensor.SetValue(HassTopicKind.State, null);
            }

            if (lastMeasurement != null)
                sensor.SetAttribute(AttributeMeasurement, lastMeasurement.Name);
        }
    }
}