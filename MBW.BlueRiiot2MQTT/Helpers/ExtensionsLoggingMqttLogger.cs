using System;
using Microsoft.Extensions.Logging;
using MQTTnet.Diagnostics;

namespace MBW.BlueRiiot2MQTT.Helpers
{
    class ExtensionsLoggingMqttLogger : IMqttNetLogger, IMqttNetScopedLogger
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly string _source;

        public ExtensionsLoggingMqttLogger(ILoggerFactory loggerFactory, string source)
        {
            _source = source;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger(source);
        }

        public IMqttNetScopedLogger CreateScopedLogger(string source)
        {
            return new ExtensionsLoggingMqttLogger(_loggerFactory, $"{_source}.{source}");
        }

        public void Publish(MqttNetLogLevel logLevel, string message, object[] parameters, Exception exception)
        {
            Publish(logLevel, _source, message, parameters, exception);
        }

        public void Publish(MqttNetLogLevel logLevel, string source, string message, object[] parameters, Exception exception)
        {
            LogLevel level;
            switch (logLevel)
            {
                case MqttNetLogLevel.Verbose:
                    level = LogLevel.Trace;
                    break;
                case MqttNetLogLevel.Info:
                    level = LogLevel.Information;
                    break;
                case MqttNetLogLevel.Warning:
                    level = LogLevel.Warning;
                    break;
                case MqttNetLogLevel.Error:
                    level = LogLevel.Error;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
            }

            _logger.Log(level, exception, message, parameters);
        }

        public event EventHandler<MqttNetLogMessagePublishedEventArgs> LogMessagePublished;
    }
}