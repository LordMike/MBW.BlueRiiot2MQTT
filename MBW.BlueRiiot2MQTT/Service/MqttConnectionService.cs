using System;
using System.Threading;
using System.Threading.Tasks;
using MBW.BlueRiiot2MQTT.Configuration;
using MBW.BlueRiiot2MQTT.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet.Client;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;

namespace MBW.BlueRiiot2MQTT.Service
{
    internal class MqttConnectionService : BackgroundService
    {
        private readonly ILogger<MqttConnectionService> _logger;
        private readonly MqttEvents _mqttEvents;
        private readonly IMqttClient _mqttClient;
        private readonly IMqttClientOptions _mqttConnectOptions;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly MqttConfiguration _mqttConfig;

        public MqttConnectionService(ILogger<MqttConnectionService> logger, MqttEvents mqttEvents, IMqttClient mqttClient, IMqttClientOptions mqttConnectOptions, IOptions<MqttConfiguration> mqttConfig, IHostApplicationLifetime lifetime)
        {
            _logger = logger;
            _mqttEvents = mqttEvents;
            _mqttClient = mqttClient;
            _mqttConnectOptions = mqttConnectOptions;
            _mqttConfig = mqttConfig.Value;
            _lifetime = lifetime;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            CancellationToken appStopToken = _lifetime.ApplicationStopping;

            _mqttEvents.OnDisconnect += async (args, token) =>
            {
                // Do not reconnect if we're done
                if (appStopToken.IsCancellationRequested)
                    return;

                _logger.LogWarning(args.Exception, "Server disconnected, attempting reconnect in {Time}", _mqttConfig.ReconnectInterval);

                await Task.Delay(_mqttConfig.ReconnectInterval, stoppingToken);

                try
                {
                    await _mqttClient.ConnectAsync(_mqttConnectOptions, stoppingToken);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Reconnect failed");
                }
            };
            
            // Register disconnect when app closes
            appStopToken.Register(o => ((IMqttClient)o).DisconnectAsync(new MqttClientDisconnectOptions
                {
                    ReasonCode = MqttClientDisconnectReason.NormalDisconnection,
                    ReasonString = "Shutting down"
                }), _mqttClient);

            return Task.CompletedTask;
        }
    }
}