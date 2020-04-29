using System;
using System.Collections.Generic;
using EnumsNET;
using MBW.BlueRiiot2MQTT.HASS.Enum;
using MBW.BlueRiiot2MQTT.Helpers;
using Newtonsoft.Json.Linq;
using Serilog;
using uPLibrary.Networking.M2Mqtt;

namespace MBW.BlueRiiot2MQTT.HASS
{
    internal class HassMqttSensor
    {
        private readonly ILogger _logger;
        private readonly string _discoveryTopic;
        private readonly HassDeviceClass _deviceClass;
        private readonly string _stateTopic;
        private readonly string _attributesTopic;

        private JObject _discover = new JObject();
        private bool _discoverDirty = true;

        private bool _attributesDirty = false;
        private readonly Dictionary<string, object> _attributes = new Dictionary<string, object>();

        private bool _valueDirty = false;
        private object _value = default;

        public string Name { get; }
        public string UniqueId { get; }

        public HassMqttSensor(string discoveryTopic, string topicPrefix, string name, string uniqueId, HassDeviceClass deviceClass)
        {
            _discoveryTopic = discoveryTopic;
            _deviceClass = deviceClass;
            _stateTopic = topicPrefix + "/state";
            _attributesTopic = topicPrefix + "/attr";

            Name = name;
            UniqueId = uniqueId;

            _discover["name"] = Name;
            _discover["unique_id"] = UniqueId;
            _discover["state_topic"] = _stateTopic;
            _discover["json_attributes_topic"] = _attributesTopic;
            _discover["device"] = new JObject();

            if (_deviceClass != HassDeviceClass.None)
                _discover["device_class"] = _deviceClass.AsString(EnumFormat.EnumMemberValue);

            _logger = Log
                .ForContext("sensor_id", uniqueId)
                .ForContext<HassMqttSensor>();
        }

        public void FlushIfNeeded(MqttClient mqttClient)
        {
            if (_discoverDirty)
            {
                _logger.Debug("Publishing discovery doc to {topic} for {uniqueId}", _discoveryTopic, UniqueId);
                mqttClient.SendJson(_discoveryTopic, _discover);

                _discoverDirty = false;
            }

            if (_attributesDirty)
            {
                _logger.Debug("Publishing attributes change to {topic} for {uniqueId}", _attributesTopic, UniqueId);
                mqttClient.SendJson(_attributesTopic, JToken.FromObject(_attributes));
                _attributesDirty = false;
            }

            if (_valueDirty)
            {
                _logger.Debug("Publishing state change to {topic} for {uniqueId}", _stateTopic, UniqueId);

                if (TryConvertValue(_value, out string str))
                    mqttClient.SendValue(_stateTopic, str);
                else
                    mqttClient.SendValue(_stateTopic, JToken.FromObject(_value));

                _valueDirty = false;
            }
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

            identifiers.Add(id);

            return this;
        }

        public HassMqttSensor SetProperty(HassMqttSensorProperty property, string value)
        {
            if (value == null)
                _discover.Remove(property.AsString(EnumFormat.EnumMemberValue));
            else
                _discover[property.AsString(EnumFormat.EnumMemberValue)] = value;

            _discoverDirty = true;

            return this;
        }

        public HassMqttSensor SetDeviceProperty(HassMqttSensorDeviceProperty deviceProperty, string value)
        {
            JToken device = _discover["device"];

            device[deviceProperty.AsString(EnumFormat.EnumMemberValue)] = value;
            _discoverDirty = true;

            return this;
        }

        public void SetAttribute(string name, object value)
        {
            if (value == default)
            {
                if (_attributes.Remove(name))
                    _attributesDirty = true;

                return;
            }

            if (_attributes.TryGetValue(name, out object existing) && object.Equals(existing, value))
                return;

            _logger.Verbose("Setting attribute {name} to {value}, for {uniqueId}", name, value, UniqueId);

            _attributes[name] = value;
            _attributesDirty = true;
        }

        public void SetValue(object value)
        {
            if (Equals(value, _value))
                return;

            _logger.Verbose("Setting value {value}, for {uniqueId}", value, UniqueId);

            _value = value;
            _valueDirty = true;
        }
    }
}