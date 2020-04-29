using JetBrains.Annotations;
using MBW.Client.BlueRiiotApi.Objects;

namespace MBW.BlueRiiot2MQTT.Features
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
    internal abstract class FeatureUpdaterBase
    {
        protected SensorStore SensorStore { get; }

        protected FeatureUpdaterBase(SensorStore sensorStore)
        {
            SensorStore = sensorStore;
        }

        public void Update(SwimmingPool pool, object obj)
        {
            if (!AppliesTo(pool, obj))
                return;

            string uniqueId = GetUniqueId(pool, obj);

            if (!SensorStore.IsDiscovered(uniqueId))
                CreateSensor(pool, uniqueId, obj);

            UpdateInternal(pool, uniqueId, obj);
        }

        protected abstract bool AppliesTo(SwimmingPool pool, object obj);

        protected abstract string GetUniqueId(SwimmingPool pool, object obj);

        protected abstract void CreateSensor(SwimmingPool pool, string uniqueId, object obj);

        protected abstract void UpdateInternal(SwimmingPool pool, string uniqueId, object obj);
    }

    internal abstract class FeatureUpdaterBaseTyped<T> : FeatureUpdaterBase where T : class
    {
        protected FeatureUpdaterBaseTyped(SensorStore sensorStore)
            : base(sensorStore)
        {
        }

        protected sealed override bool AppliesTo(SwimmingPool pool, object obj)
        {
            return obj is T asT && AppliesTo(pool, asT);
        }

        protected sealed override string GetUniqueId(SwimmingPool pool, object obj)
        {
            return GetUniqueId(pool, (T)obj);
        }

        protected sealed override void CreateSensor(SwimmingPool pool, string uniqueId, object obj)
        {
            CreateSensor(pool, uniqueId, (T)obj);
        }

        protected sealed override void UpdateInternal(SwimmingPool pool, string uniqueId, object obj)
        {
            UpdateInternal(pool, uniqueId, (T)obj);
        }

        protected virtual bool AppliesTo(SwimmingPool pool, T obj)
        {
            return true;
        }

        protected abstract string GetUniqueId(SwimmingPool pool, T obj);

        protected abstract void CreateSensor(SwimmingPool pool, string uniqueId, T obj);

        protected abstract void UpdateInternal(SwimmingPool pool, string uniqueId, T obj);
    }
}