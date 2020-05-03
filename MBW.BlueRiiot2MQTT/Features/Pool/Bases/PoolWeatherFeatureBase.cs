using MBW.BlueRiiot2MQTT.HASS;
using MBW.BlueRiiot2MQTT.HASS.Enum;
using MBW.BlueRiiot2MQTT.Helpers;
using MBW.Client.BlueRiiotApi.Objects;

namespace MBW.BlueRiiot2MQTT.Features.Pool.Bases
{
    internal abstract class PoolWeatherFeatureBase : FeatureUpdaterBaseTyped<SwimmingPoolWeather>
    {
        private readonly string _name;
        private readonly string _measurement;
        private readonly string _unit;
        private readonly HassDeviceClass _deviceClass;

        public PoolWeatherFeatureBase(SensorStore sensorStore, string name, string measurement, string unit, HassDeviceClass deviceClass = HassDeviceClass.None) : base(sensorStore)
        {
            _name = name;
            _measurement = measurement;
            _unit = unit;
            _deviceClass = deviceClass;
        }

        protected override string GetUniqueId(SwimmingPool pool, SwimmingPoolWeather obj)
        {
            return $"pool_{pool.SwimmingPoolId}_{_measurement}";
        }

        protected override void CreateSensor(SwimmingPool pool, string uniqueId, SwimmingPoolWeather obj)
        {
            SensorStore.Create($"{pool.Name} {_name}", uniqueId, HassDeviceType.Sensor, $"pool_{pool.SwimmingPoolId}", _measurement, _deviceClass)
                .SetHassProperties(pool)
                .SetProperty(HassMqttSensorProperty.UnitOfMeasurement, _unit);
        }

        protected sealed override void UpdateInternal(SwimmingPool pool, string uniqueId, SwimmingPoolWeather obj)
        {
            HassMqttSensor sensor = SensorStore.Get(uniqueId);

            sensor.SetAttribute("timestamp", obj.Timestamp);

            Update(pool, uniqueId, obj);
        }

        protected abstract void Update(SwimmingPool pool, string uniqueId, SwimmingPoolWeather obj);

        internal class PoolWeatherTempFeature : PoolWeatherFeatureBase
        {
            public PoolWeatherTempFeature(SensorStore sensorStore) : base(sensorStore, "Weather forecast Temperature", "weather_temp", "°C", HassDeviceClass.Temperature)
            {
            }

            protected override void Update(SwimmingPool pool, string uniqueId, SwimmingPoolWeather obj)
            {
                HassMqttSensor sensor = SensorStore.Get(uniqueId);

                sensor.SetAttribute("temp_min", obj.TemperatureMin);
                sensor.SetAttribute("temp_max", obj.TemperatureMax);

                sensor.SetValue(obj.TemperatureCurrent);
            }
        }

        internal class PoolWeatherUvFeature : PoolWeatherFeatureBase
        {
            public PoolWeatherUvFeature(SensorStore sensorStore) : base(sensorStore, "Weather forecast UV index", "weather_uv", "UV index")
            {
            }

            protected override void Update(SwimmingPool pool, string uniqueId, SwimmingPoolWeather obj)
            {
                HassMqttSensor sensor = SensorStore.Get(uniqueId);

                sensor.SetValue(obj.UvCurrent);
            }
        }

        internal class PoolWeatherFeature : PoolWeatherFeatureBase
        {
            public PoolWeatherFeature(SensorStore sensorStore) : base(sensorStore, "Weather forecast", "weather", null)
            {
            }

            protected override void Update(SwimmingPool pool, string uniqueId, SwimmingPoolWeather obj)
            {
                HassMqttSensor sensor = SensorStore.Get(uniqueId);

                sensor.SetAttribute("temp_min", obj.TemperatureMin);
                sensor.SetAttribute("temp_max", obj.TemperatureMax);
                sensor.SetAttribute("temp", obj.TemperatureCurrent);
                sensor.SetAttribute("wind_speed", obj.WindSpeedCurrent);
                sensor.SetAttribute("uv", obj.UvCurrent);

                sensor.SetValue(obj.WeatherCurrentDescription);
            }
        }
    }
}