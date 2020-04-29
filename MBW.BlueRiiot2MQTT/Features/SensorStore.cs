using System.Collections.Generic;
using MBW.BlueRiiot2MQTT.HASS;
using MBW.BlueRiiot2MQTT.HASS.Enum;
using uPLibrary.Networking.M2Mqtt;

namespace MBW.BlueRiiot2MQTT.Features
{
    internal class SensorStore
    {
        private readonly HassTopicBuilder _topicBuilder;
        private readonly MqttClient _mqttClient;
        private Dictionary<string, HassMqttSensor> _sensors;

        public SensorStore(HassTopicBuilder topicBuilder, MqttClient mqttClient)
        {
            _topicBuilder = topicBuilder;
            _mqttClient = mqttClient;
            _sensors = new Dictionary<string, HassMqttSensor>();
        }

        public HassMqttSensor Create(string name, string uniqueId, HassDeviceType deviceType, string deviceId, string entityId, HassDeviceClass deviceClass)
        {
            string discoveryTopic = _topicBuilder.GetDiscoveryTopic(deviceType, deviceId, entityId);
            string topicPrefix = _topicBuilder.GetDeviceTopicPrefix(deviceType, deviceId, entityId);

            HassMqttSensor sensor = new HassMqttSensor(discoveryTopic, topicPrefix, name, uniqueId, deviceClass);

            _sensors.Add(uniqueId, sensor);

            return sensor;
        }

        public void FlushAll()
        {
            foreach (HassMqttSensor sensor in _sensors.Values)
                sensor.FlushIfNeeded(_mqttClient);
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