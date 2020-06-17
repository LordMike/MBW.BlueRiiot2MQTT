using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MBW.BlueRiiot2MQTT.Configuration;
using MBW.BlueRiiot2MQTT.HASS;
using MBW.BlueRiiot2MQTT.HASS.Enum;
using Microsoft.Extensions.Options;
using MQTTnet.Client;

namespace MBW.BlueRiiot2MQTT.Features
{
    internal class SensorStore
    {
        private readonly HassTopicBuilder _topicBuilder;
        private readonly BlueRiiotConfiguration _blueRiiotConfiguration;
        private readonly Dictionary<string, HassMqttSensor> _sensors;

        public SensorStore(HassTopicBuilder topicBuilder, IOptions<BlueRiiotConfiguration> blueRiiotConfiguration)
        {
            _topicBuilder = topicBuilder;
            _blueRiiotConfiguration = blueRiiotConfiguration.Value;
            _sensors = new Dictionary<string, HassMqttSensor>();
        }

        public HassMqttSensor Create(string name, string uniqueId, HassDeviceType deviceType, string deviceId, string entityId, HassDeviceClass deviceClass)
        {
            string discoveryTopic = _topicBuilder.GetDiscoveryTopic(deviceType, deviceId, entityId);
            string topicPrefix = _topicBuilder.GetDeviceTopicPrefix(deviceType, deviceId, entityId);

            HassMqttSensor sensor = new HassMqttSensor(discoveryTopic, topicPrefix, name, uniqueId, deviceClass);
            sensor.EnableReportingUnchangedValues = _blueRiiotConfiguration.ReportUnchangedValues;

            _sensors.Add(uniqueId, sensor);

            return sensor;
        }

        public async Task FlushAll(IMqttClient mqttClient, CancellationToken token = default)
        {
            foreach (HassMqttSensor sensor in _sensors.Values)
                await sensor.FlushIfNeeded(mqttClient, token);
        }

        public HassMqttSensor Get(string uniqueId)
        {
            return _sensors[uniqueId];
        }

        public bool IsDiscovered(string uniqueId)
        {
            return _sensors.ContainsKey(uniqueId);
        }
    }
}