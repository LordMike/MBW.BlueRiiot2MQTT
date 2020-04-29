using System;
using System.Collections.Generic;
using System.Linq;
using MBW.BlueRiiot2MQTT.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MBW.BlueRiiot2MQTT.Helpers
{
    internal static class Extensions
    {
        public static TOptions GetOptions<TOptions>(this IServiceProvider provider) where TOptions : class, new()
        {
            return provider.GetRequiredService<IOptions<TOptions>>().Value;
        }

        public static ILogger<T> GetLogger<T>(this IServiceProvider provider)
        {
            return provider.GetRequiredService<ILogger<T>>();
        }

        public static ILogger GetLogger(this IServiceProvider provider, Type type)
        {
            return provider.GetRequiredService<ILoggerFactory>().CreateLogger(type);
        }

        public static IServiceCollection AddAllFeatureUpdaters(this IServiceCollection services)
        {
            IEnumerable<Type> types = typeof(Extensions).Assembly
                .GetTypes()
                .Where(s => !s.IsAbstract)
                .Where(s => typeof(FeatureUpdaterBase).IsAssignableFrom(s));

            foreach (Type type in types)
                services.AddSingleton(typeof(FeatureUpdaterBase), type);

            return services;
        }
    }
}