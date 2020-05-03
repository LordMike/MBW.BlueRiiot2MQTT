using System.Collections.Generic;
using System.Linq;
using EnumsNET;
using MBW.BlueRiiot2MQTT.Features.Enums;
using MBW.BlueRiiot2MQTT.Features.Pool.Bases;
using MBW.BlueRiiot2MQTT.HASS;
using MBW.BlueRiiot2MQTT.HASS.Enum;
using MBW.BlueRiiot2MQTT.Helpers;
using MBW.Client.BlueRiiotApi.Objects;
using MBW.Client.BlueRiiotApi.RequestsResponses;

namespace MBW.BlueRiiot2MQTT.Features.Pool
{
    internal abstract class PoolMeasurementFeature : LastMeasurementsFeatureBase
    {
        private readonly string _name;
        private readonly string _key;
        private readonly string _measurement;
        private readonly string _unit;

        public PoolMeasurementFeature(SensorStore sensorStore, string name, string measurement, string unit) : base(sensorStore)
        {
            _name = name;
            _key = measurement.ToLower();
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

        protected override string GetUniqueId(SwimmingPool pool, List<SwimmingPoolLastMeasurementsGetResponse> measurements, SwimmingPoolLastMeasurementsGetResponse latest)
        {
            return $"pool_{pool.SwimmingPoolId}_{_key}";
        }

        protected override void CreateSensor(SwimmingPool pool, string uniqueId, List<SwimmingPoolLastMeasurementsGetResponse> measurements, SwimmingPoolLastMeasurementsGetResponse latest)
        {
            SensorStore.Create($"{pool.Name} {_name}", uniqueId, HassDeviceType.Sensor, $"pool_{pool.SwimmingPoolId}", _measurement, HassDeviceClass.None)
                .SetHassProperties(pool)
                .SetProperty(HassMqttSensorProperty.UnitOfMeasurement, _unit);
        }

        protected override void UpdateInternal(SwimmingPool pool, string uniqueId, List<SwimmingPoolLastMeasurementsGetResponse> measurements, SwimmingPoolLastMeasurementsGetResponse latest)
        {
            if (!TryGetMeasurement(latest.Data, out SwpLastMeasurements measurement))
                return;

            HassMqttSensor sensor = SensorStore.Get(uniqueId);

            MeasurementUtility.AddAttributes(sensor, measurement);

            MeasurementStatus status = MeasurementUtility.GetStatus(measurement);
            sensor.SetAttribute("status", status.AsString(EnumFormat.EnumMemberValue));

            sensor.SetValue(measurement.Value);
        }

        internal class PoolTaFeature : PoolMeasurementFeature
        {
            public PoolTaFeature(SensorStore sensorStore) : base(sensorStore, "Total Alkalinity", "ta", "mg/L")
            {
            }
        }

        internal class PoolPhFeature : PoolMeasurementFeature
        {
            public PoolPhFeature(SensorStore sensorStore) : base(sensorStore, "pH", "ph", "pH")
            {
            }
        }

        internal class PoolCyaFeature : PoolMeasurementFeature
        {
            public PoolCyaFeature(SensorStore sensorStore) : base(sensorStore, "Cyuranic Acid", "cya", "mg/L")
            {
            }
        }

        internal class PoolTemperatureFeature : PoolMeasurementFeature
        {
            public PoolTemperatureFeature(SensorStore sensorStore) : base(sensorStore, "Temperature", "temperature", "°C")
            {
            }
        }

        internal class PoolConductivityFeature : PoolMeasurementFeature
        {
            public PoolConductivityFeature(SensorStore sensorStore) : base(sensorStore, "Conductivity", "conductivity", "µS")
            {
            }
        }

        internal class PoolOrpFeature : PoolMeasurementFeature
        {
            public PoolOrpFeature(SensorStore sensorStore) : base(sensorStore, "ORP", "orp", "mV")
            {
            }
        }
    }
}