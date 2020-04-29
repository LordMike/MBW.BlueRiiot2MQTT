using System;
using System.Threading;
using System.Threading.Tasks;
using MBW.BlueRiiot2MQTT.Configuration;
using MBW.BlueRiiot2MQTT.Features;
using MBW.Client.BlueRiiotApi;
using MBW.Client.BlueRiiotApi.Objects;
using MBW.Client.BlueRiiotApi.RequestsResponses;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using uPLibrary.Networking.M2Mqtt;

namespace MBW.BlueRiiot2MQTT.Service
{
    internal class BlueRiiotMqttService : BackgroundService
    {
        private readonly ILogger<BlueRiiotMqttService> _logger;
        private readonly BlueClient _blueClient;
        private readonly MqttClient _mqttClient;
        private readonly FeatureUpdateManager _updateManager;
        private readonly BlueRiiotConfiguration _config;

        public BlueRiiotMqttService(
            ILogger<BlueRiiotMqttService> logger,
            IOptions<BlueRiiotConfiguration> config,
            BlueClient blueClient, MqttClient mqttClient,
            FeatureUpdateManager updateManager)
        {
            _logger = logger;
            _blueClient = blueClient;
            _mqttClient = mqttClient;
            _updateManager = updateManager;
            _config = config.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Update loop
            DateTime lastRun = DateTime.MinValue;

            while (!stoppingToken.IsCancellationRequested)
            {
                TimeSpan toDelay = _config.UpdateInterval - (DateTime.UtcNow - lastRun);
                if (toDelay > TimeSpan.Zero)
                    await Task.Delay(toDelay, stoppingToken);

                _logger.LogDebug("Beginning update");

                try
                {
                    await PerformUpdate(stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An error occurred while performing the update");
                }

                _updateManager.FlushIfNeeded();

                lastRun = DateTime.UtcNow;
            }
        }

        private async Task PerformUpdate(CancellationToken stoppingToken)
        {
            SwimmingPoolGetResponse pools = await _blueClient.GetSwimmingPools(token: stoppingToken);

            foreach (UserSwimmingPool userPool in pools.Data)
            {
                SwimmingPool pool = userPool.SwimmingPool;
                _updateManager.Update(pool, pool);

                _logger.LogDebug("Fetching measurements for {Id} ({Name})", pool.SwimmingPoolId, pool.Name);

                SwimmingPoolLastMeasurementsGetResponse measurements = await _blueClient.GetSwimmingPoolLastMeasurements(pool.SwimmingPoolId, stoppingToken);
                _updateManager.Update(pool, measurements);
                _updateManager.Update(pool, measurements.Data);
                
                _logger.LogDebug("Fetching weather for {Id} ({Name})", pool.SwimmingPoolId, pool.Name);

                SwimmingPoolWeatherGetResponse weather = await _blueClient.GetSwimmingPoolWeather(pool.SwimmingPoolId, _config.Language, stoppingToken);
                _updateManager.Update(pool, weather.Data);

                //_logger.LogDebug("Fetching status for {Id} ({Name})", pool.SwimmingPoolId, pool.Name);
                //SwimmingPoolStatusGetResponse status = await _blueClient.GetSwimmingPoolStatus(pool.SwimmingPoolId, stoppingToken);
                //_updateManager.Update(pool, status);
            }
        }
    }
}