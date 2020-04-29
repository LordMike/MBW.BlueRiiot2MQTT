using EnumsNET;
using MBW.BlueRiiot2MQTT.Configuration;
using MBW.BlueRiiot2MQTT.HASS.Enum;
using Microsoft.Extensions.Options;

namespace MBW.BlueRiiot2MQTT.HASS
{
    internal class HassTopicBuilder
    {
        private readonly string _discoveryPrefix;
        private readonly string _blueRiiotPrefix;

        public HassTopicBuilder(IOptions<HassConfiguration> hassOptions)
        {
            HassConfiguration config = hassOptions.Value;

            _discoveryPrefix = config.DiscoveryPrefix;
            if (!string.IsNullOrEmpty(_discoveryPrefix) && !_discoveryPrefix.EndsWith('/'))
                _discoveryPrefix += "/";

            _blueRiiotPrefix = config.BlueRiiotPrefix;
            if (!string.IsNullOrEmpty(_blueRiiotPrefix) && !_blueRiiotPrefix.EndsWith('/'))
                _blueRiiotPrefix += "/";
        }

        public string GetDiscoveryTopic(HassDeviceType deviceType, string deviceId, string entityId)
        {
            return $"{_discoveryPrefix}{deviceType.AsString(EnumFormat.EnumMemberValue)}/{deviceId}/{entityId}/config";
        }

        public string GetDeviceTopicPrefix(HassDeviceType deviceType, string deviceId, string entityId)
        {
            return $"{_blueRiiotPrefix}{deviceType.AsString(EnumFormat.EnumMemberValue)}/{deviceId}/{entityId}";
        }
    }
}