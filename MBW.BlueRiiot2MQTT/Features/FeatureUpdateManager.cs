using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MBW.Client.BlueRiiotApi.Objects;
using MQTTnet.Client;

namespace MBW.BlueRiiot2MQTT.Features
{
    internal class FeatureUpdateManager
    {
        private readonly SensorStore _sensorStore;
        private readonly IMqttClient _mqttClient;
        private readonly List<FeatureUpdaterBase> _updaters;

        public FeatureUpdateManager(SensorStore sensorStore, IMqttClient mqttClient, IEnumerable<FeatureUpdaterBase> updaters)
        {
            _sensorStore = sensorStore;
            _mqttClient = mqttClient;
            _updaters = updaters.ToList();
        }

        public void Update(SwimmingPool pool, object obj)
        {
            foreach (FeatureUpdaterBase updater in _updaters)
                updater.Update(pool, obj);
        }

        public Task FlushIfNeeded(CancellationToken token = default)
        {
            return _sensorStore.FlushAll(_mqttClient, token);
        }
    }
}