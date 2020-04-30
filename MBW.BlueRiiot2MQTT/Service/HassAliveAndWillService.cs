using System.Threading;
using System.Threading.Tasks;
using MBW.BlueRiiot2MQTT.HASS;
using MBW.BlueRiiot2MQTT.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet.Client;

namespace MBW.BlueRiiot2MQTT.Service
{
    internal class HassAliveAndWillService : BackgroundService
    {
        private readonly ILogger<HassAliveAndWillService> _logger;
        private readonly IMqttClient _mqttClient;
        private readonly MqttEvents _mqttEvents;
        private readonly HassWillContainer _willContainer;

        public HassAliveAndWillService(ILogger<HassAliveAndWillService> logger, IMqttClient mqttClient, MqttEvents mqttEvents, HassWillContainer willContainer)
        {
            _logger = logger;
            _mqttClient = mqttClient;
            _mqttEvents = mqttEvents;
            _willContainer = willContainer;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _mqttEvents.OnConnect += async (args, token) =>
            {
                if (stoppingToken.IsCancellationRequested)
                    return;

                await _mqttClient.SendValueAsync(_willContainer.StateTopic, HassWillContainer.RunningMessage, token);
            };

            return Task.CompletedTask;
        }
    }
}