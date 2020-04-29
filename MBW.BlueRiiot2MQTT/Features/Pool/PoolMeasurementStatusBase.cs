using System.Collections.Generic;
using System.Linq;
using EnumsNET;
using MBW.BlueRiiot2MQTT.HASS;
using MBW.BlueRiiot2MQTT.HASS.Enum;
using MBW.BlueRiiot2MQTT.Helpers;
using MBW.Client.BlueRiiotApi.Objects;

namespace MBW.BlueRiiot2MQTT.Features.Pool
{
    internal abstract class PoolMeasurementStatusBase : FeatureUpdaterBaseTyped<List<SwpLastMeasurements>>
    {
        private readonly string _name;
        private readonly string _key;
        private readonly string _measurement;

        public PoolMeasurementStatusBase(SensorStore sensorStore, string name, string measurement) : base(sensorStore)
        {
            _name = name;
            _key = measurement.ToLower();
            _measurement = measurement;
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
            return $"pool_{pool.SwimmingPoolId}_{_key}_status";
        }

        protected override void CreateSensor(SwimmingPool pool, string uniqueId, List<SwpLastMeasurements> measurements)
        {
            SensorStore.Create($"{pool.Name} {_name}", uniqueId, HassDeviceType.Sensor, $"pool_{pool.SwimmingPoolId}", $"{_measurement}_status", HassDeviceClass.None)
                .SetHassProperties(pool);
        }

        protected override void UpdateInternal(SwimmingPool pool, string uniqueId, List<SwpLastMeasurements> measurements)
        {
            if (!TryGetMeasurement(measurements, out SwpLastMeasurements measurement))
                return;

            HassMqttSensor sensor = SensorStore.Get(uniqueId);

            MeasurementUtility.AddAttributes(sensor, measurement);

            MeasurementStatus status = MeasurementUtility.GetStatus(measurement);
            sensor.SetValue(status.AsString(EnumFormat.EnumMemberValue));
        }

        internal class PoolTaFeature : PoolMeasurementStatusBase
        {
            public PoolTaFeature(SensorStore sensorStore) : base(sensorStore, "Total Alkalinity status", "ta")
            {
            }
        }

        internal class PoolPhFeature : PoolMeasurementStatusBase
        {
            public PoolPhFeature(SensorStore sensorStore) : base(sensorStore, "pH status", "ph")
            {
            }
        }

        internal class PoolCyaFeature : PoolMeasurementStatusBase
        {
            public PoolCyaFeature(SensorStore sensorStore) : base(sensorStore, "Cyuranic Acid status", "cya")
            {
            }
        }

        internal class PoolTemperatureFeature : PoolMeasurementStatusBase
        {
            public PoolTemperatureFeature(SensorStore sensorStore) : base(sensorStore, "Temperature status", "temperature")
            {
            }
        }
    }
}