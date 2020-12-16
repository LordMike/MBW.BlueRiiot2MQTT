using System.Threading;
using System.Threading.Tasks;
using MBW.BlueRiiot2MQTT.Service;
using MBW.Client.BlueRiiotApi;
using MBW.Client.BlueRiiotApi.RequestsResponses;
using MBW.HassMQTT.CommonServices.Commands;
using Microsoft.Extensions.Logging;
using MQTTnet;

namespace MBW.BlueRiiot2MQTT.Commands
{
    internal class ReleaseLastUnprocessedCommand : IMqttCommandHandler
    {
        private readonly ILogger<ReleaseLastUnprocessedCommand> _logger;
        private readonly BlueClient _client;
        private readonly BlueRiiotMqttService _service;

        public ReleaseLastUnprocessedCommand(ILogger<ReleaseLastUnprocessedCommand> logger, BlueClient client, BlueRiiotMqttService service)
        {
            _logger = logger;
            _client = client;
            _service = service;
        }

        public string[] GetFilter()
        {
            return new[] { "commands", "release_last_unprocessed", null };
        }

        public async Task Handle(string[] topicLevels, MqttApplicationMessage message, CancellationToken token = new CancellationToken())
        {
            string serial = topicLevels[2];

            _logger.LogDebug("Calling BlueReleaseLastUnprocessedEvent for {Serial}", serial);

            BlueReleaseLastUnprocessedEventResponse res = await _client.BlueReleaseLastUnprocessedEvent(serial, token);

            if (res.Code == 200)
            {
                _logger.LogInformation("Successfully called BlueReleaseLastUnprocessedEvent for {Serial}", serial);

                _service.ForceSync();
            }
            else
                _logger.LogError("Error calling BlueReleaseLastUnprocessedEvent for {Serial}: {Code}, {Message}", serial, res.Code, res.Message);
        }
    }
}