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
    internal abstract class PoolMeasurementFeature : LastMeasurementsFeatureBase
    {
        private readonly string _displayName;
        private readonly string _measurement;
        private readonly string _unit;

        public PoolMeasurementFeature(HassMqttManager hassMqttManager, string displayName, string measurement, string unit) : base(hassMqttManager)
        {
            _displayName = displayName;
            _measurement = measurement;
            _unit = unit;
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
            HassMqttManager.ConfigureSensor<MqttSensor>(HassUniqueIdBuilder.GetPoolDeviceId(pool), _measurement)
                .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
                .SetHassPoolProperties(pool)
                .ConfigureDiscovery(discovery =>
                {
                    discovery.Name = $"{pool.Name} {_displayName}";
                    discovery.UnitOfMeasurement = _unit;
                })
                .ConfigureAliveService();
        }

        protected override void UpdateInternal(SwimmingPool pool, List<SwimmingPoolLastMeasurementsGetResponse> measurements, SwimmingPoolLastMeasurementsGetResponse latest)
        {
            if (!TryGetMeasurement(latest.Data, out SwpLastMeasurements measurement))
                return;

            ISensorContainer sensor = HassMqttManager.GetSensor(HassUniqueIdBuilder.GetPoolDeviceId(pool), _measurement);
            MqttAttributesTopic attributesSender = sensor.GetAttributesSender();

            MeasurementUtility.AddAttributes(attributesSender, measurement);

            MeasurementStatus status = MeasurementUtility.GetStatus(measurement);
            attributesSender.SetAttribute("status", status.AsString(EnumFormat.EnumMemberValue));

            sensor.SetValue(HassTopicKind.State, measurement.Value);
        }
        
        [UsedImplicitly]
        internal class PoolTaFeature : PoolMeasurementFeature
        {
            public PoolTaFeature(HassMqttManager hassMqttManager) : base(hassMqttManager, "Total Alkalinity", "ta", "mg/L")
            {
            }
        }
        
        [UsedImplicitly]
        internal class PoolPhFeature : PoolMeasurementFeature
        {
            public PoolPhFeature(HassMqttManager hassMqttManager) : base(hassMqttManager, "pH", "ph", "pH")
            {
            }
        }
        
        [UsedImplicitly]
        internal class PoolCyaFeature : PoolMeasurementFeature
        {
            public PoolCyaFeature(HassMqttManager hassMqttManager) : base(hassMqttManager, "Cyuranic Acid", "cya", "mg/L")
            {
            }
        }
        
        [UsedImplicitly]
        internal class PoolTemperatureFeature : PoolMeasurementFeature
        {
            public PoolTemperatureFeature(HassMqttManager hassMqttManager) : base(hassMqttManager, "Temperature", "temperature", "°C")
            {
            }
        }
        
        [UsedImplicitly]
        internal class PoolConductivityFeature : PoolMeasurementFeature
        {
            public PoolConductivityFeature(HassMqttManager hassMqttManager) : base(hassMqttManager, "Conductivity", "conductivity", "µS")
            {
            }
        }
        
        [UsedImplicitly]
        internal class PoolOrpFeature : PoolMeasurementFeature
        {
            public PoolOrpFeature(HassMqttManager hassMqttManager) : base(hassMqttManager, "ORP", "orp", "mV")
            {
            }
        }
        
        [UsedImplicitly]
        internal class PoolSalinityFeature : PoolMeasurementFeature
        {
            public PoolSalinityFeature(HassMqttManager hassMqttManager) : base(hassMqttManager, "Salinity", "salinity", "g/L")
            {
            }
        }
        
        [UsedImplicitly]
        internal class PoolFreeChlorineFeature : PoolMeasurementFeature
        {
            public PoolFreeChlorineFeature(HassMqttManager hassMqttManager) : base(hassMqttManager, "Free Chlorine", "fcl", "ppm")
            {
            }
        }
        
        [UsedImplicitly]
        internal class PoolFreeBromineFeature : PoolMeasurementFeature
        {
            public PoolFreeBromineFeature(HassMqttManager hassMqttManager) : base(hassMqttManager, "Free Bromine", "fbr", "ppm")
            {
            }
        }
    }
}