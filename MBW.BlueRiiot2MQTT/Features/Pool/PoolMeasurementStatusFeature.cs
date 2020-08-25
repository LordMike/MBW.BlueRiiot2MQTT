using System.Collections.Generic;
using System.Linq;
using EnumsNET;
using JetBrains.Annotations;
using MBW.BlueRiiot2MQTT.Features.Enums;
using MBW.BlueRiiot2MQTT.Features.Pool.Bases;
using MBW.BlueRiiot2MQTT.HASS;
using MBW.BlueRiiot2MQTT.Helpers;
using MBW.Client.BlueRiiotApi.Objects;
using MBW.Client.BlueRiiotApi.RequestsResponses;
using MBW.HassMQTT;
using MBW.HassMQTT.CommonServices.AliveAndWill;
using MBW.HassMQTT.DiscoveryModels.Enum;
using MBW.HassMQTT.DiscoveryModels.Models;
using MBW.HassMQTT.Extensions;
using MBW.HassMQTT.Interfaces;

namespace MBW.BlueRiiot2MQTT.Features.Pool
{
    [UsedImplicitly]
    internal abstract class PoolMeasurementStatusFeature : LastMeasurementsFeatureBase
    {
        private readonly string _displayName;
        private readonly string _measurement;

        public PoolMeasurementStatusFeature(HassMqttManager hassMqttManager, string displayName, string measurement) : base(hassMqttManager)
        {
            _displayName = displayName;
            _measurement = measurement;
        }

        private bool TryGetMeasurement(List<SwpLastMeasurements> measurements, out SwpLastMeasurements measurement)
        {
            measurement = measurements.FirstOrDefault(s => !s.Expired && s.Name == _measurement);
            return measurement != null;
        }

        protected override bool AppliesTo(SwimmingPool pool, List<SwimmingPoolLastMeasurementsGetResponse> measurements, SwimmingPoolLastMeasurementsGetResponse latest)
        {
            return latest != null && TryGetMeasurement(latest.Data, out _);
        }

        protected override void CreateSensor(SwimmingPool pool, List<SwimmingPoolLastMeasurementsGetResponse> measurements, SwimmingPoolLastMeasurementsGetResponse latest)
        {
            HassMqttManager.ConfigureSensor<MqttSensor>(HassUniqueIdBuilder.GetPoolDeviceId(pool), $"{_measurement}_status")
                .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
                .SetHassPoolProperties(pool)
                .ConfigureDiscovery(discovery =>
                {
                    discovery.Name = $"{pool.Name} {_displayName}";
                })
                .ConfigureAliveService();
        }

        protected override void UpdateInternal(SwimmingPool pool, List<SwimmingPoolLastMeasurementsGetResponse> measurements, SwimmingPoolLastMeasurementsGetResponse latest)
        {
            if (!TryGetMeasurement(latest.Data, out SwpLastMeasurements measurement))
                return;

            ISensorContainer sensor = HassMqttManager
                .GetSensor(HassUniqueIdBuilder.GetPoolDeviceId(pool), $"{_measurement}_status")
                .SetPoolAttributes(pool);

            MeasurementUtility.AddAttributes(sensor.GetAttributesSender(), measurement);

            MeasurementStatus status = MeasurementUtility.GetStatus(measurement);
            sensor.SetValue(HassTopicKind.State, status.AsString(EnumFormat.EnumMemberValue));
        }
        
        [UsedImplicitly]
        internal class PoolTaFeature : PoolMeasurementStatusFeature
        {
            public PoolTaFeature(HassMqttManager hassMqttManager) : base(hassMqttManager, "Total Alkalinity status", "ta")
            {
            }
        }
        
        [UsedImplicitly]
        internal class PoolPhFeature : PoolMeasurementStatusFeature
        {
            public PoolPhFeature(HassMqttManager hassMqttManager) : base(hassMqttManager, "pH status", "ph")
            {
            }
        }
        
        [UsedImplicitly]
        internal class PoolCyaFeature : PoolMeasurementStatusFeature
        {
            public PoolCyaFeature(HassMqttManager hassMqttManager) : base(hassMqttManager, "Cyuranic Acid status", "cya")
            {
            }
        }
        
        [UsedImplicitly]
        internal class PoolTemperatureFeature : PoolMeasurementStatusFeature
        {
            public PoolTemperatureFeature(HassMqttManager hassMqttManager) : base(hassMqttManager, "Temperature status", "temperature")
            {
            }
        }
        
        [UsedImplicitly]
        internal class PoolConductivityFeature : PoolMeasurementStatusFeature
        {
            public PoolConductivityFeature(HassMqttManager hassMqttManager) : base(hassMqttManager, "Conductivity status", "conductivity")
            {
            }
        }
        
        [UsedImplicitly]
        internal class PoolOrpFeature : PoolMeasurementStatusFeature
        {
            public PoolOrpFeature(HassMqttManager hassMqttManager) : base(hassMqttManager, "OR statusP", "orp")
            {
            }
        }
        
        [UsedImplicitly]
        internal class PoolSalinityFeature : PoolMeasurementStatusFeature
        {
            public PoolSalinityFeature(HassMqttManager hassMqttManager) : base(hassMqttManager, "Salinity status", "salinity")
            {
            }
        }
        
        [UsedImplicitly]
        internal class PoolFreeChlorineFeature : PoolMeasurementStatusFeature
        {
            public PoolFreeChlorineFeature(HassMqttManager hassMqttManager) : base(hassMqttManager, "Free Chlorine status", "fcl")
            {
            }
        }
        
        [UsedImplicitly]
        internal class PoolFreeBromineFeature : PoolMeasurementStatusFeature
        {
            public PoolFreeBromineFeature(HassMqttManager hassMqttManager) : base(hassMqttManager, "Free Bromine status", "fbr")
            {
            }
        }
    }
}