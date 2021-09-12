using MBW.BlueRiiot2MQTT.HASS;
using MBW.BlueRiiot2MQTT.Helpers;
using MBW.Client.BlueRiiotApi.Objects;
using MBW.HassMQTT;
using MBW.HassMQTT.CommonServices.AliveAndWill;
using MBW.HassMQTT.DiscoveryModels.Enum;
using MBW.HassMQTT.DiscoveryModels.Models;
using MBW.HassMQTT.Extensions;
using MBW.HassMQTT.Interfaces;

namespace MBW.BlueRiiot2MQTT.Features.Pool.Bases
{
    internal abstract class PoolWeatherFeatureBase : FeatureUpdaterBaseTyped<SwimmingPoolWeather>
    {
        private readonly string _displayName;
        private readonly string _measurement;
        private readonly string _unit;
        private readonly HassSensorDeviceClass? _deviceClass;

        public PoolWeatherFeatureBase(HassMqttManager hassMqttManager, string displayName, string measurement, string unit, HassSensorDeviceClass? deviceClass = null) : base(hassMqttManager)
        {
            _displayName = displayName;
            _measurement = measurement;
            _unit = unit;
            _deviceClass = deviceClass;
        }

        protected sealed override void CreateSensor(SwimmingPool pool, SwimmingPoolWeather obj)
        {
            IDiscoveryDocumentBuilder<MqttSensor> builder = HassMqttManager.ConfigureSensor<MqttSensor>(HassUniqueIdBuilder.GetPoolDeviceId(pool), _measurement)
                .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
                .SetHassPoolProperties(pool)
                .ConfigureDiscovery(discovery =>
                {
                    discovery.Name = $"{pool.Name} {_displayName}";
                    discovery.DeviceClass = _deviceClass;
                    discovery.UnitOfMeasurement = _unit;
                })
                .ConfigureAliveService();

            ConfigureSensorInternal(builder);
        }

        protected virtual void ConfigureSensorInternal(IDiscoveryDocumentBuilder<MqttSensor> builder) { }

        protected sealed override void UpdateInternal(SwimmingPool pool, SwimmingPoolWeather obj)
        {
            ISensorContainer sensor = HassMqttManager.GetSensor(HassUniqueIdBuilder.GetPoolDeviceId(pool), _measurement);

            Update(sensor, pool, obj);

            sensor
                .SetAttribute("timestamp", obj.Timestamp)
                .SetPoolAttributes(pool);
        }

        protected abstract void Update(ISensorContainer sensor, SwimmingPool pool, SwimmingPoolWeather obj);

        internal class PoolWeatherTempFeature : PoolWeatherFeatureBase
        {
            public PoolWeatherTempFeature(HassMqttManager hassMqttManager) : base(hassMqttManager, "Weather forecast Temperature", "weather_temp", "°C", HassSensorDeviceClass.Temperature)
            {
            }

            protected override void Update(ISensorContainer sensor, SwimmingPool pool, SwimmingPoolWeather obj)
            {
                sensor.SetAttribute("temp_min", obj.TemperatureMin);
                sensor.SetAttribute("temp_max", obj.TemperatureMax);

                sensor.SetValue(HassTopicKind.State, obj.TemperatureCurrent);
            }

            protected override void ConfigureSensorInternal(IDiscoveryDocumentBuilder<MqttSensor> builder)
            {
                builder.ConfigureDiscovery(s => s.StateClass = "measurement");
            }
        }

        internal class PoolWeatherTempMinFeature : PoolWeatherFeatureBase
        {
            public PoolWeatherTempMinFeature(HassMqttManager hassMqttManager) : base(hassMqttManager, "Weather forecast Temperature (min)", "weather_temp_min", "°C", HassSensorDeviceClass.Temperature)
            {
            }

            protected override void Update(ISensorContainer sensor, SwimmingPool pool, SwimmingPoolWeather obj)
            {
                sensor.SetValue(HassTopicKind.State, obj.TemperatureMin);
            }

            protected override void ConfigureSensorInternal(IDiscoveryDocumentBuilder<MqttSensor> builder)
            {
                builder.ConfigureDiscovery(s => s.StateClass = "measurement");
            }
        }

        internal class PoolWeatherTempMaxFeature : PoolWeatherFeatureBase
        {
            public PoolWeatherTempMaxFeature(HassMqttManager hassMqttManager) : base(hassMqttManager, "Weather forecast Temperature (max)", "weather_temp_max", "°C", HassSensorDeviceClass.Temperature)
            {
            }

            protected override void Update(ISensorContainer sensor, SwimmingPool pool, SwimmingPoolWeather obj)
            {
                sensor.SetValue(HassTopicKind.State, obj.TemperatureMax);
            }

            protected override void ConfigureSensorInternal(IDiscoveryDocumentBuilder<MqttSensor> builder)
            {
                builder.ConfigureDiscovery(s => s.StateClass = "measurement");
            }
        }

        internal class PoolWeatherUvFeature : PoolWeatherFeatureBase
        {
            public PoolWeatherUvFeature(HassMqttManager hassMqttManager) : base(hassMqttManager, "Weather forecast UV index", "weather_uv", "UV index")
            {
            }

            protected override void Update(ISensorContainer sensor, SwimmingPool pool, SwimmingPoolWeather obj)
            {
                sensor.SetValue(HassTopicKind.State, obj.UvCurrent);
            }

            protected override void ConfigureSensorInternal(IDiscoveryDocumentBuilder<MqttSensor> builder)
            {
                builder.ConfigureDiscovery(s => s.StateClass = "measurement");
            }
        }

        internal class PoolWeatherFeature : PoolWeatherFeatureBase
        {
            public PoolWeatherFeature(HassMqttManager hassMqttManager) : base(hassMqttManager, "Weather forecast", "weather", null)
            {
            }

            protected override void Update(ISensorContainer sensor, SwimmingPool pool, SwimmingPoolWeather obj)
            {
                sensor.SetAttribute("temp_min", obj.TemperatureMin);
                sensor.SetAttribute("temp_max", obj.TemperatureMax);
                sensor.SetAttribute("temp", obj.TemperatureCurrent);
                sensor.SetAttribute("wind_speed", obj.WindSpeedCurrent);
                sensor.SetAttribute("uv", obj.UvCurrent);

                sensor.SetValue(HassTopicKind.State, obj.WeatherCurrentDescription);
            }
        }
    }
}