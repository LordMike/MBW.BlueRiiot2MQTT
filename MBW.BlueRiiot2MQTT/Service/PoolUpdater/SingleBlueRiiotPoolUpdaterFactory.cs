using MBW.BlueRiiot2MQTT.Configuration;
using MBW.BlueRiiot2MQTT.Features;
using MBW.Client.BlueRiiotApi;
using MBW.Client.BlueRiiotApi.Objects;
using MBW.HassMQTT;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MBW.BlueRiiot2MQTT.Service.PoolUpdater
{
    internal class SingleBlueRiiotPoolUpdaterFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly HassMqttManager _hassMqttManager;
        private readonly FeatureUpdateManager _updateManager;
        private readonly BlueClient _blueClient;
        private readonly BlueRiiotConfiguration _config;

        public SingleBlueRiiotPoolUpdaterFactory(ILoggerFactory loggerFactory, HassMqttManager hassMqttManager, FeatureUpdateManager updateManager, BlueClient blueClient, IOptions<BlueRiiotConfiguration> config)
        {
            _loggerFactory = loggerFactory;
            _hassMqttManager = hassMqttManager;
            _updateManager = updateManager;
            _blueClient = blueClient;
            _config = config.Value;
        }

        public SingleBlueRiiotPoolUpdater Create(SwimmingPool pool)
        {
            // TODO: Add structured logging property to indicate pool
            ILogger<SingleBlueRiiotPoolUpdater> logger = _loggerFactory.CreateLogger<SingleBlueRiiotPoolUpdater>();

            return new SingleBlueRiiotPoolUpdater(logger, _hassMqttManager, _updateManager, _blueClient, _config, pool);
        }
    }
}