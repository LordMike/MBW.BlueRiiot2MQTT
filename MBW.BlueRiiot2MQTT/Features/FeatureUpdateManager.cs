using System.Collections.Generic;
using System.Linq;
using MBW.Client.BlueRiiotApi.Objects;

namespace MBW.BlueRiiot2MQTT.Features
{
    internal class FeatureUpdateManager
    {
        private readonly SensorStore _sensorStore;
        private readonly List<FeatureUpdaterBase> _updaters;

        public FeatureUpdateManager(SensorStore sensorStore, IEnumerable<FeatureUpdaterBase> updaters)
        {
            _sensorStore = sensorStore;
            _updaters = updaters.ToList();
        }

        public void Update(SwimmingPool pool, object obj)
        {
            foreach (FeatureUpdaterBase updater in _updaters)
                updater.Update(pool, obj);
        }

        public void FlushIfNeeded()
        {
            _sensorStore.FlushAll();
        }
    }
}