using System;
using System.Linq;
using Humanizer;
using MBW.BlueRiiot2MQTT.HASS;
using MBW.BlueRiiot2MQTT.Helpers;
using MBW.Client.BlueRiiotApi.Objects;
using MBW.HassMQTT;
using MBW.HassMQTT.CommonServices.AliveAndWill;
using MBW.HassMQTT.DiscoveryModels.Enum;
using MBW.HassMQTT.DiscoveryModels.Models;
using MBW.HassMQTT.Extensions;
using MBW.HassMQTT.Interfaces;

namespace MBW.BlueRiiot2MQTT.Features.BlueDevice
{
    internal class BlueDeviceFeature : FeatureUpdaterBaseTyped<SwimmingPoolDevice>
    {
        public BlueDeviceFeature(HassMqttManager hassMqttManager) : base(hassMqttManager)
        {
        }

        protected override bool AppliesTo(SwimmingPool pool, SwimmingPoolDevice data)
        {
            return data.BlueDevice != null;
        }

        protected override void CreateSensor(SwimmingPool pool, SwimmingPoolDevice data)
        {
            string deviceId = HassUniqueIdBuilder.GetBlueDeviceId(data);
            string deviceName = $"Blue {data.BlueDevice.HwType.Humanize(LetterCasing.Title)} v{data.BlueDevice.HwGeneration} ({data.BlueDevice.HwRegion.Humanize(LetterCasing.AllCaps)}) Device {data.BlueDevice.Serial}";
            string namePrefix = $"Blue {data.BlueDeviceSerial}";

            SensorExtensions.DeviceConfigure deviceConfigure = device =>
            {
                if (!device.Identifiers.Contains(deviceId))
                    device.Identifiers.Add(deviceId);

                device.Name = deviceName;
                device.SwVersion = data.BlueDevice.FwVersionPsoc;
                device.Manufacturer = "Blue Riiot";
                device.Model = $"Blue {data.BlueDevice.HwType.Humanize(LetterCasing.Title)} v{data.BlueDevice.HwGeneration} ({data.BlueDevice.HwRegion.Humanize(LetterCasing.AllCaps)})";
            };

            HassMqttManager.ConfigureSensor<MqttSensor>(deviceId, "device")
                .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
                .ConfigureDevice(deviceConfigure)
                .ConfigureDiscovery(discovery =>
                {
                    discovery.DeviceClass = HassSensorDeviceClass.Timestamp;
                    discovery.Name = $"{namePrefix} Last Contact";
                })
                .ConfigureAliveService();

            // Battery was previously done from "BatteryLow" API response
            // But this value is nonfunctional (https://github.com/LordMike/MBW.BlueRiiot2MQTT/issues/62#issuecomment-925086790)
            HassMqttManager.RemoveSensor<MqttSensor>(deviceId, "battery");

            HassMqttManager.ConfigureSensor<MqttSensor>(deviceId, "status")
                .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
                .ConfigureDevice(deviceConfigure)
                .ConfigureDiscovery(discovery =>
                {
                    discovery.Name = $"{namePrefix} Status";
                })
                .ConfigureAliveService();
        }

        protected override void UpdateInternal(SwimmingPool pool, SwimmingPoolDevice data)
        {
            string deviceId = HassUniqueIdBuilder.GetBlueDeviceId(data);
            ISensorContainer deviceSensor = HassMqttManager
                .GetSensor(deviceId, "device")
                .SetPoolAttributes(pool);
            ISensorContainer statusSensor = HassMqttManager
                .GetSensor(deviceId, "status")
                .SetPoolAttributes(pool);

            // Device
            // Determine last contact
            DateTime? latestContact = ComparisonHelper.GetMax(data.Created,
                data.BlueDevice.LastHelloMessageV,
                data.BlueDevice.LastMeasureMessage,
                data.BlueDevice.LastMeasureMessageBle,
                data.BlueDevice.LastMeasureMessageSigfox);

            deviceSensor.SetValue(HassTopicKind.State, latestContact);

            // Device attributes
            deviceSensor.SetAttribute("serial", data.BlueDevice.Serial);
            deviceSensor.SetAttribute("serial_number", data.BlueDevice.Sn);
            deviceSensor.SetAttribute("wake_interval", data.BlueDevice.WakePeriod);
            deviceSensor.SetAttribute("firmware", data.BlueDevice.FwVersionPsoc);
            deviceSensor.SetAttribute("firmware_installed", data.BlueDevice.FwVersionHistory?.OrderByDescending(s => s.Timestamp).Select(s => s.Timestamp).FirstOrDefault());

            // Status (awake, sleeping, ..)
            statusSensor.SetValue(HassTopicKind.State, data.BlueDevice.SleepState);
        }
    }
}