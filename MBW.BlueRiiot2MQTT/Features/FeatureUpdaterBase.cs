using JetBrains.Annotations;
using MBW.Client.BlueRiiotApi.Objects;
using MBW.HassMQTT;

namespace MBW.BlueRiiot2MQTT.Features
{
    [UsedImplicitly]
    internal abstract class FeatureUpdaterBase
    {
        protected HassMqttManager HassMqttManager { get; }

        protected FeatureUpdaterBase(HassMqttManager hassMqttManager)
        {
            HassMqttManager = hassMqttManager;
        }

        public void Update(SwimmingPool pool, object obj)
        {
            if (!AppliesTo(pool, obj))
                return;

            CreateSensor(pool, obj);

            UpdateInternal(pool, obj);
        }

        protected abstract bool AppliesTo(SwimmingPool pool, object obj);

        protected abstract void CreateSensor(SwimmingPool pool, object obj);

        protected abstract void UpdateInternal(SwimmingPool pool, object obj);
    }
}