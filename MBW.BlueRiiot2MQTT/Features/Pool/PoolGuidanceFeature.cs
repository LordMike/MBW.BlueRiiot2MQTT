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
    internal class PoolGuidanceFeature : FeatureUpdaterBaseTyped<SwimmingPoolGuidanceGetResponse>
    {
        public PoolGuidanceFeature(HassMqttManager hassMqttManager) : base(hassMqttManager)
        {
        }

        protected override void CreateSensor(SwimmingPool pool, SwimmingPoolGuidanceGetResponse guidance)
        {
            HassMqttManager.ConfigureSensor<MqttSensor>(HassUniqueIdBuilder.GetPoolDeviceId(pool), "guidance")
                .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
                .SetHassPoolProperties(pool)
                .ConfigureDiscovery(discovery =>
                {
                    discovery.Name = $"{pool.Name} Guidance";
                })
                .ConfigureAliveService();
        }

        protected override void UpdateInternal(SwimmingPool pool, SwimmingPoolGuidanceGetResponse guidance)
        {
            ISensorContainer sensor = HassMqttManager
                .GetSensor(HassUniqueIdBuilder.GetPoolDeviceId(pool), "guidance")
                .SetPoolAttributes(pool);

            if (guidance.Guidance?.IssueToFix == null)
            {
                sensor.SetValue(HassTopicKind.State, "No guidance at this time");
                sensor.SetAttribute("status", "ok");

                return;
            }

            string text = $"{guidance.Guidance.IssueToFix.IssueTitle}: {guidance.Guidance.IssueToFix.ActionTitle}";

            sensor.SetValue(HassTopicKind.State, text);
            sensor.SetAttribute("status", "alert");
        }
    }
}