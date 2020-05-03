using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnumsNET;
using MBW.BlueRiiot2MQTT.HASS.Enum;
using MBW.BlueRiiot2MQTT.Helpers;
using MQTTnet.Client;
using Newtonsoft.Json.Linq;
using Serilog;

namespace MBW.BlueRiiot2MQTT.HASS
{
    internal class HassMqttSensor
    {
        private readonly ILogger _logger;
        private readonly string _discoveryTopic;
        private readonly HassDeviceClass _deviceClass;

        private JObject _discover = new JObject();
        private bool _discoverDirty = true;

        private bool _attributesDirty = false;
        private readonly Dictionary<string, object> _attributes = new Dictionary<string, object>();

        private bool _valueDirty = false;
        private object _value = default;

        public string Name { get; }
        public string UniqueId { get; }
        public string StateTopic { get; }
        public string AttributesTopic { get; }

        public HassMqttSensor(string discoveryTopic, string topicPrefix, string name, string uniqueId, HassDeviceClass deviceClass)
        {
            _discoveryTopic = discoveryTopic;
            _deviceClass = deviceClass;
            StateTopic = topicPrefix + "/state";
            AttributesTopic = topicPrefix + "/attr";

            Name = name;
            UniqueId = uniqueId;

            _discover["name"] = Name;
            _discover["unique_id"] = UniqueId;
            _discover["state_topic"] = StateTopic;
            _discover["json_attributes_topic"] = AttributesTopic;
            _discover["device"] = new JObject();

            if (_deviceClass != HassDeviceClass.None)
                _discover["device_class"] = _deviceClass.AsString(EnumFormat.EnumMemberValue);

            _logger = Log
                .ForContext("sensor_id", uniqueId)
                .ForContext<HassMqttSensor>();
        }

        public async Task<bool> FlushIfNeeded(IMqttClient mqttClient, CancellationToken token)
        {
            bool anyFlushed = false;

            if (_discoverDirty)
            {
                _logger.Debug("Publishing discovery doc to {topic} for {uniqueId}", _discoveryTopic, UniqueId);
                await mqttClient.SendJsonAsync(_discoveryTopic, _discover, token);

                _discoverDirty = false;
                anyFlushed = true;
            }

            if (_attributesDirty)
            {
                _logger.Debug("Publishing attributes change to {topic} for {uniqueId}", AttributesTopic, UniqueId);
                await mqttClient.SendJsonAsync(AttributesTopic, JToken.FromObject(_attributes), token);

                _attributesDirty = false;
                anyFlushed = true;
            }

            if (_valueDirty)
            {
                _logger.Debug("Publishing state change to {topic} for {uniqueId}", StateTopic, UniqueId);

                if (TryConvertValue(_value, out string str))
                    await mqttClient.SendValueAsync(StateTopic, str, token);
                else
                    await mqttClient.SendJsonAsync(StateTopic, JToken.FromObject(_value), token);

                _valueDirty = false;
                anyFlushed = true;
            }

            if (anyFlushed)
                _logger.Information("Sent new values for {Name} to MQTT", _discover.Value<string>("name"));

            return anyFlushed;
        }

        private static bool TryConvertValue(object val, out string str)
        {
            switch (val)
            {
                case DateTime asDateTime:
                    str = asDateTime.ToString("O");
                    return true;
                case string asString:
                    str = asString;
                    return true;
                default:
                    str = null;
                    return false;
            }
        }

        public HassMqttSensor AddDeviceIdentifier(string id)
        {
            JToken device = _discover["device"];
            JArray identifiers = device["identifiers"] as JArray;

            if (identifiers == null)
                device["identifiers"] = identifiers = new JArray();
            
            _logger.Verbose("Adding device identifier {identifier} for {uniqueId}", id, UniqueId);

            identifiers.Add(id);
            _discoverDirty = true;

            return this;
        }

        public HassMqttSensor SetProperty(HassMqttSensorProperty property, string value)
        {
            string? propertyName = property.AsString(EnumFormat.EnumMemberValue);

            if (value == null)
                _discover.Remove(propertyName);
            else
                _discover[propertyName] = value;

            _logger.Verbose("Setting property {name} to {value}, for {uniqueId}", propertyName, value, UniqueId);

            _discoverDirty = true;

            return this;
        }

        public HassMqttSensor SetDeviceProperty(HassMqttSensorDeviceProperty deviceProperty, string value)
        {
            JToken device = _discover["device"];

            string? propertyName = deviceProperty.AsString(EnumFormat.EnumMemberValue);

            _logger.Verbose("Setting device property {name} to {value}, for {uniqueId}", propertyName, value, UniqueId);

            device[propertyName] = value;
            _discoverDirty = true;

            return this;
        }

        public HassMqttSensor SetAttribute(string name, object value)
        {
            if (value == default)
            {
                if (_attributes.Remove(name))
                    _attributesDirty = true;

                return this;
            }

            if (_attributes.TryGetValue(name, out object existing) && ComparisonHelper.IsSameValue(existing, value))
                return this;

            _logger.Verbose("Setting attribute {name} to {value}, for {uniqueId}", name, value, UniqueId);

            _attributes[name] = value;
            _attributesDirty = true;

            return this;
        }

        public void SetValue(object value)
        {
            if (ComparisonHelper.IsSameValue(value, _value))
                return;

            _logger.Verbose("Setting value {value}, for {uniqueId}", value, UniqueId);

            _value = value;
            _valueDirty = true;
        }
    }
}