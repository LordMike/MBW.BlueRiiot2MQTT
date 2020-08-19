using System.Threading;
using System.Threading.Tasks;
using MBW.BlueRiiot2MQTT.Service;
using MBW.HassMQTT.CommonServices.Commands;
using MQTTnet;

namespace MBW.BlueRiiot2MQTT.Commands
{
    internal class ForceSyncCommand : IMqttCommandHandler
    {
        private readonly BlueRiiotMqttService _service;

        public ForceSyncCommand(BlueRiiotMqttService service)
        {
            _service = service;
        }

        public string[] GetFilter()
        {
            return new[] { "commands", "force_sync" };
        }

        public Task Handle(string[] topicLevels, MqttApplicationMessage message, CancellationToken token = new CancellationToken())
        {
            _service.ForceSync();

            return Task.CompletedTask;
        }
    }
}