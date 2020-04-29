using System.Collections.Generic;
using System.Linq;
using EnumsNET;
using MBW.BlueRiiot2MQTT.HASS;
using MBW.BlueRiiot2MQTT.HASS.Enum;
using MBW.BlueRiiot2MQTT.Helpers;
using MBW.Client.BlueRiiotApi.Objects;

namespace MBW.BlueRiiot2MQTT.Features.Pool
{
    internal abstract class PoolMeasurementBase : FeatureUpdaterBaseTyped<List<SwpLastMeasurements>>
    {
        private readonly string _name;
        private readonly string _key;
        private readonly string _measurement;
        private readonly string _unit;

        public PoolMeasurementBase(SensorStore sensorStore, string name, string measurement, string unit) : base(sensorStore)
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

        protected override bool AppliesTo(SwimmingPool pool, List<SwpLastMeasurements> obj)
        {
            return TryGetMeasurement(obj, out _);
        }

        protected override string GetUniqueId(SwimmingPool pool, List<SwpLastMeasurements> measurements)
        {
            return $"pool_{pool.SwimmingPoolId}_{_key}";
        }

        protected override void CreateSensor(SwimmingPool pool, string uniqueId, List<SwpLastMeasurements> measurements)
        {
            SensorStore.Create($"{pool.Name} {_name}", uniqueId, HassDeviceType.Sensor, $"pool_{pool.SwimmingPoolId}", _measurement, HassDeviceClass.None)
                .SetHassProperties(pool)
                .SetProperty(HassMqttSensorProperty.UnitOfMeasurement, _unit);
        }

        protected override void UpdateInternal(SwimmingPool pool, string uniqueId, List<SwpLastMeasurements> measurements)
        {
            if (!TryGetMeasurement(measurements, out SwpLastMeasurements measurement))
                return;

            HassMqttSensor sensor = SensorStore.Get(uniqueId);

            MeasurementUtility.AddAttributes(sensor, measurement);

            MeasurementStatus status = MeasurementUtility.GetStatus(measurement);
            sensor.SetAttribute("status", status.AsString(EnumFormat.EnumMemberValue));

            sensor.SetValue(measurement.Value);
        }

        internal class PoolTaFeature : PoolMeasurementBase
        {
            public PoolTaFeature(SensorStore sensorStore) : base(sensorStore, "Total Alkalinity", "ta", "mg/L")
            {
            }
        }

        internal class PoolPhFeature : PoolMeasurementBase
        {
            public PoolPhFeature(SensorStore sensorStore) : base(sensorStore, "pH", "ph", "pH")
            {
            }
        }

        internal class PoolCyaFeature : PoolMeasurementBase
        {
            public PoolCyaFeature(SensorStore sensorStore) : base(sensorStore, "Cyuranic Acid", "cya", "mg/L")
            {
            }
        }

        internal class PoolTemperatureFeature : PoolMeasurementBase
        {
            public PoolTemperatureFeature(SensorStore sensorStore) : base(sensorStore, "Temperature", "temperature", "°C")
            {
            }
        }
    }
}