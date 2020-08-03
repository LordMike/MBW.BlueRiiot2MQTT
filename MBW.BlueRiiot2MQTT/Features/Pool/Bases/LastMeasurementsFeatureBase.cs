using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MBW.BlueRiiot2MQTT.Helpers;
using MBW.Client.BlueRiiotApi.Objects;
using MBW.Client.BlueRiiotApi.RequestsResponses;
using MBW.HassMQTT;

namespace MBW.BlueRiiot2MQTT.Features.Pool.Bases
{
    [UsedImplicitly]
    internal abstract class LastMeasurementsFeatureBase : FeatureUpdaterBaseTyped<List<SwimmingPoolLastMeasurementsGetResponse>>
    {
        protected LastMeasurementsFeatureBase(HassMqttManager hassMqttManager) : base(hassMqttManager)
        {
        }

        protected sealed override bool AppliesTo(SwimmingPool pool, List<SwimmingPoolLastMeasurementsGetResponse> obj)
        {
            SwimmingPoolLastMeasurementsGetResponse latest = obj.OrderByDescending(s => ComparisonHelper.GetMax(s.LastStripTimestamp, s.LastBlueMeasureTimestamp)).FirstOrDefault();

            return AppliesTo(pool, obj, latest);
        }

        protected sealed override void CreateSensor(SwimmingPool pool, List<SwimmingPoolLastMeasurementsGetResponse> obj)
        {
            SwimmingPoolLastMeasurementsGetResponse latest = obj.OrderByDescending(s => ComparisonHelper.GetMax(s.LastStripTimestamp, s.LastBlueMeasureTimestamp)).FirstOrDefault();

            CreateSensor(pool, obj, latest);
        }

        protected sealed override void UpdateInternal(SwimmingPool pool, List<SwimmingPoolLastMeasurementsGetResponse> obj)
        {
            SwimmingPoolLastMeasurementsGetResponse latest = obj.OrderByDescending(s => ComparisonHelper.GetMax(s.LastStripTimestamp, s.LastBlueMeasureTimestamp)).FirstOrDefault();

            UpdateInternal(pool, obj, latest);
        }

        protected virtual bool AppliesTo(SwimmingPool pool, List<SwimmingPoolLastMeasurementsGetResponse> measurements, SwimmingPoolLastMeasurementsGetResponse latest)
        {
            return true;
        }

        protected abstract void UpdateInternal(SwimmingPool pool, List<SwimmingPoolLastMeasurementsGetResponse> measurements, SwimmingPoolLastMeasurementsGetResponse latest);

        protected abstract void CreateSensor(SwimmingPool pool, List<SwimmingPoolLastMeasurementsGetResponse> measurements, SwimmingPoolLastMeasurementsGetResponse latest);
    }
}