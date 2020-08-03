using System.Collections.Generic;
using System.Linq;
using MBW.Client.BlueRiiotApi.Objects;

namespace MBW.BlueRiiot2MQTT.Features
{
    internal class FeatureUpdateManager
    {
        private readonly List<FeatureUpdaterBase> _updaters;

        public FeatureUpdateManager(IEnumerable<FeatureUpdaterBase> updaters)
        {
            _updaters = updaters.ToList();
        }

        public void Process(SwimmingPool pool, object obj)
        {
            foreach (FeatureUpdaterBase updater in _updaters)
                updater.Update(pool, obj);
        }
    }
}