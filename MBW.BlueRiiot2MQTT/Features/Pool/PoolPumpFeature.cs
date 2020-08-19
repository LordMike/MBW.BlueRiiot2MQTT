using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MBW.BlueRiiot2MQTT.HASS;
using MBW.BlueRiiot2MQTT.Helpers;
using MBW.Client.BlueRiiotApi.Objects;
using MBW.HassMQTT;
using MBW.HassMQTT.CommonServices.AliveAndWill;
using MBW.HassMQTT.DiscoveryModels.Enum;
using MBW.HassMQTT.DiscoveryModels.Models;
using MBW.HassMQTT.Extensions;
using MBW.HassMQTT.Interfaces;

namespace MBW.BlueRiiot2MQTT.Features.Pool
{
    [UsedImplicitly]
    internal class PoolPumpFeature : FeatureUpdaterBaseTyped<SwimmingPool>
    {
        const string TimeFormat = @"hh\:mm";

        public PoolPumpFeature(HassMqttManager hassMqttManager) : base(hassMqttManager)
        {
        }

        protected override void CreateSensor(SwimmingPool pool, SwimmingPool _)
        {
            HassMqttManager.ConfigureSensor<MqttSensor>(HassUniqueIdBuilder.GetPoolDeviceId(pool), "pump_schedule")
                .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
                .SetHassPoolProperties(pool)
                .ConfigureDiscovery(discovery =>
                {
                    discovery.Name = $"{pool.Name} Pump Schedule";
                })
                .ConfigureAliveService();
        }

        protected override void UpdateInternal(SwimmingPool pool, SwimmingPool _)
        {
            ISensorContainer sensor = HassMqttManager.GetSensor(HassUniqueIdBuilder.GetPoolDeviceId(pool), "pump_schedule");

            SwimmingPoolCharacteristicsFilterPump pump = pool.Characteristics?.FilterPump;
            if (pump == null)
                return;

            MqttAttributesTopic attributes = sensor.GetAttributesSender();

            // The new schedule may have fewer time ranges
            attributes.Clear();

            sensor.SetPoolAttributes(pool);

            if (!pump.IsPresent)
            {
                // No pump
                sensor.SetValue(HassTopicKind.State, "None");
            }
            else if (pump.OperatingType == "Manual")
            {
                // Pump exists, but is not scheduled
                sensor.SetValue(HassTopicKind.State, "Manual");
            }
            else if (pump.OperatingType == "Scheduled")
            {
                sensor.SetValue(HassTopicKind.State, "Scheduled");

                if (pump.OperatingHours != null && pump.OperatingHours.Any())
                {
                    string allTimes = string.Join(", ", pump.OperatingHours.Select(s => s.Start.ToString(TimeFormat) + "-" + s.End.ToString(TimeFormat)));
                    attributes.SetAttribute("schedule", allTimes);

                    attributes.SetAttribute("schedules", pump.OperatingHours.Count);

                    for (int index = 0; index < pump.OperatingHours.Count; index++)
                    {
                        TimeRange range = pump.OperatingHours[index];

                        attributes.SetAttribute($"schedule_{index}", $"{range.Start.ToString(TimeFormat)}-{range.End.ToString(TimeFormat)}");
                        attributes.SetAttribute($"schedule_{index}_start", range.Start.ToString(TimeFormat));
                        attributes.SetAttribute($"schedule_{index}_end", range.End.ToString(TimeFormat));
                    }
                }
            }
        }
    }
}