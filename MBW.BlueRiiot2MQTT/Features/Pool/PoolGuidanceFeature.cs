using MBW.BlueRiiot2MQTT.HASS;
using MBW.BlueRiiot2MQTT.HASS.Enum;
using MBW.BlueRiiot2MQTT.Helpers;
using MBW.Client.BlueRiiotApi.Objects;
using MBW.Client.BlueRiiotApi.RequestsResponses;

namespace MBW.BlueRiiot2MQTT.Features.Pool
{
    internal class PoolGuidanceFeature : FeatureUpdaterBaseTyped<SwimmingPoolGuidanceGetResponse>
    {
        public PoolGuidanceFeature(SensorStore sensorStore) : base(sensorStore)
        {
        }

        protected override string GetUniqueId(SwimmingPool pool, SwimmingPoolGuidanceGetResponse guidance)
        {
            return $"pool_{pool.SwimmingPoolId}_guidance";
        }

        protected override void CreateSensor(SwimmingPool pool, string uniqueId, SwimmingPoolGuidanceGetResponse guidance)
        {
            SensorStore.Create($"{pool.Name} Guidance", uniqueId, HassDeviceType.Sensor, $"pool_{pool.SwimmingPoolId}", "guidance", HassDeviceClass.None)
                .SetHassProperties(pool);
        }

        protected override void UpdateInternal(SwimmingPool pool, string uniqueId, SwimmingPoolGuidanceGetResponse guidance)
        {
            HassMqttSensor sensor = SensorStore.Get(uniqueId);

            if (guidance.Guidance == null)
            {
                sensor.SetValue("No guidance at this time");
                sensor.SetAttribute("status", "ok");

                return;
            }

            string text = guidance.Guidance.IssueToFix.IssueTitle + ": " + guidance.Guidance.IssueToFix.ActionTitle; 

            sensor.SetValue(text);
            sensor.SetAttribute("status", "alert");
        }
    }
}