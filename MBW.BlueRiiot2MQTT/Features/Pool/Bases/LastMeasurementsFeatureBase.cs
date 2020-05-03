using System;
using System.Collections.Generic;
using System.Linq;
using MBW.Client.BlueRiiotApi.Objects;
using MBW.Client.BlueRiiotApi.RequestsResponses;

namespace MBW.BlueRiiot2MQTT.Features.Pool.Bases
{
    internal abstract class LastMeasurementsFeatureBase : FeatureUpdaterBaseTyped<List<SwimmingPoolLastMeasurementsGetResponse>>
    {
        protected LastMeasurementsFeatureBase(SensorStore sensorStore) : base(sensorStore)
        {
        }

        protected sealed override bool AppliesTo(SwimmingPool pool, List<SwimmingPoolLastMeasurementsGetResponse> obj)
        {
            SwimmingPoolLastMeasurementsGetResponse latest = obj.OrderByDescending(s => GetMax(s.LastStripTimestamp, s.LastBlueMeasureTimestamp)).FirstOrDefault();

            return AppliesTo(pool, obj, latest);
        }

        protected sealed override void CreateSensor(SwimmingPool pool, string uniqueId, List<SwimmingPoolLastMeasurementsGetResponse> obj)
        {
            SwimmingPoolLastMeasurementsGetResponse latest = obj.OrderByDescending(s => GetMax(s.LastStripTimestamp, s.LastBlueMeasureTimestamp)).FirstOrDefault();

            CreateSensor(pool, uniqueId, obj, latest);
        }

        protected sealed override string GetUniqueId(SwimmingPool pool, List<SwimmingPoolLastMeasurementsGetResponse> obj)
        {
            SwimmingPoolLastMeasurementsGetResponse latest = obj.OrderByDescending(s => GetMax(s.LastStripTimestamp, s.LastBlueMeasureTimestamp)).FirstOrDefault();

            return GetUniqueId(pool, obj, latest);
        }

        protected sealed override void UpdateInternal(SwimmingPool pool, string uniqueId, List<SwimmingPoolLastMeasurementsGetResponse> obj)
        {
            SwimmingPoolLastMeasurementsGetResponse latest = obj.OrderByDescending(s => GetMax(s.LastStripTimestamp, s.LastBlueMeasureTimestamp)).FirstOrDefault();

            UpdateInternal(pool, uniqueId, obj, latest);
        }

        protected virtual bool AppliesTo(SwimmingPool pool, List<SwimmingPoolLastMeasurementsGetResponse> measurements, SwimmingPoolLastMeasurementsGetResponse latest)
        {
            return true;
        }

        protected abstract void UpdateInternal(SwimmingPool pool, string uniqueId, List<SwimmingPoolLastMeasurementsGetResponse> measurements, SwimmingPoolLastMeasurementsGetResponse latest);

        protected abstract void CreateSensor(SwimmingPool pool, string uniqueId, List<SwimmingPoolLastMeasurementsGetResponse> measurements, SwimmingPoolLastMeasurementsGetResponse latest);

        protected abstract string GetUniqueId(SwimmingPool pool, List<SwimmingPoolLastMeasurementsGetResponse> measurements, SwimmingPoolLastMeasurementsGetResponse latest);

        private DateTime? GetMax(DateTime? a, DateTime? b)
        {
            if (!a.HasValue)
                return b;

            if (!b.HasValue)
                return a;

            return a.Value > b.Value ? a : b;
        }
    }
}