﻿using MBW.Client.BlueRiiotApi.Objects;
using MBW.HassMQTT;

namespace MBW.BlueRiiot2MQTT.Features
{
    internal abstract class FeatureUpdaterBaseTyped<T> : FeatureUpdaterBase where T : class
    {
        protected FeatureUpdaterBaseTyped(HassMqttManager hassMqttManager)
            : base(hassMqttManager)
        {
        }

        protected sealed override bool AppliesTo(SwimmingPool pool, object obj)
        {
            return obj is T asT && AppliesTo(pool, asT);
        }

        protected sealed override void CreateSensor(SwimmingPool pool, object obj)
        {
            CreateSensor(pool, (T)obj);
        }

        protected sealed override void UpdateInternal(SwimmingPool pool, object obj)
        {
            UpdateInternal(pool, (T)obj);
        }

        protected virtual bool AppliesTo(SwimmingPool pool, T obj)
        {
            return true;
        }

        protected abstract void CreateSensor(SwimmingPool pool, T obj);

        protected abstract void UpdateInternal(SwimmingPool pool, T obj);
    }
}