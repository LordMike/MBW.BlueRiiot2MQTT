﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using MBW.BlueRiiot2MQTT.Commands;
using MBW.BlueRiiot2MQTT.Configuration;
using MBW.BlueRiiot2MQTT.Features;
using MBW.BlueRiiot2MQTT.Helpers;
using MBW.BlueRiiot2MQTT.Service;
using MBW.BlueRiiot2MQTT.Service.PoolUpdater;
using MBW.Client.BlueRiiotApi.Builder;
using MBW.HassMQTT;
using MBW.HassMQTT.CommonServices;
using MBW.HassMQTT.CommonServices.Commands;
using MBW.HassMQTT.Topics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Serilog;
using WebProxy = System.Net.WebProxy;

namespace MBW.BlueRiiot2MQTT
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            // Logging to use before logging configuration is read
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .CreateLogger();

            await CreateHostBuilder(args).RunConsoleAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(builder =>
                {
                    builder.AddJsonFile("appsettings.local.json", true);

                    string extraConfigFile = Environment.GetEnvironmentVariable("EXTRA_CONFIG_FILE");

                    if (extraConfigFile != null)
                    {
                        Log.Logger.Information("Loading extra config file at {path}", extraConfigFile);
                        builder.AddJsonFile(extraConfigFile, true);
                    }

                    // Add Docker Secrets
                    builder.AddKeyPerFile("/run/secrets", true);
                })
                .ConfigureLogging((context, builder) =>
                {
                    Log.Logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(context.Configuration, "Logging")
                        .CreateLogger();

                    builder
                        .ClearProviders()
                        .AddSerilog();
                })
                .ConfigureServices(ConfigureServices);

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services
                .AddAndConfigureMqtt("BlueRiiot2MQTT", configuration =>
                {
                    BlueRiiotHassConfiguration blueRiiotConfig = context.Configuration.GetSection("HASS").Get<BlueRiiotHassConfiguration>();
                    configuration.SendDiscoveryDocuments = blueRiiotConfig.EnableHASSDiscovery;
                    configuration.ValidateDiscoveryDocuments = true;
                })
                .Configure<CommonMqttConfiguration>(x => x.ClientId = "blueriiot2mqtt")
                .Configure<CommonMqttConfiguration>(context.Configuration.GetSection("MQTT"));

            // Commands
            services
                .AddMqttCommandService()
                .AddMqttCommandHandler<ForceSyncCommand>()
                .AddMqttCommandHandler<ReleaseLastUnprocessedCommand>()
                .AddMqttCommandHandler<SetPumpScheduleCommand>();

            services
                .Configure<BlueRiiotHassConfiguration>(context.Configuration.GetSection("HASS"))
                .Configure<HassConfiguration>(context.Configuration.GetSection("HASS"))
                .Configure<BlueRiiotConfiguration>(context.Configuration.GetSection("BlueRiiot"))
                .PostConfigure<BlueRiiotConfiguration>(x =>
                {
                    void Set<T>(Expression<Func<BlueRiiotConfiguration, T>> prop, T otherValue)
                    {
                        PropertyInfo myProp = prop.GetPropertyInfo();
                        T currentVal = (T)myProp.GetValue(x);

                        if (EqualityComparer<T>.Default.Equals(currentVal, default))
                            myProp.SetValue(x, otherValue);
                    }

                    // Copy over defaults
                    Set(n => n.UpdateIntervalWhenAllDevicesAsleep, x.UpdateInterval);

                    // Weather intervals use default intervals if not set
                    Set(n => n.WeatherUpdateInterval, x.UpdateInterval);
                    Set(n => n.WeatherUpdateIntervalWhenAllDevicesAsleep, x.UpdateIntervalWhenAllDevicesAsleep);
                })
                .Configure<ProxyConfiguration>(context.Configuration.GetSection("Proxy"))
                .AddSingleton(x => new HassMqttTopicBuilder(x.GetOptions<HassConfiguration>()))
                .AddHttpClient("blueriiot")
                .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(60)
                }))
                .ConfigurePrimaryHttpMessageHandler(provider =>
                {
                    ProxyConfiguration proxyConfig = provider.GetOptions<ProxyConfiguration>();

                    SocketsHttpHandler handler = new SocketsHttpHandler();

                    if (proxyConfig.Uri != null)
                        handler.Proxy = new WebProxy(proxyConfig.Uri);

                    return handler;
                })
                .Services
                .AddBlueRiiotClient((provider, builder) =>
                {
                    IHttpClientFactory httpFactory = provider.GetRequiredService<IHttpClientFactory>();
                    BlueRiiotConfiguration config = provider.GetOptions<BlueRiiotConfiguration>();

                    builder
                        .UseUsernamePassword(config.Username, config.Password)
                        .UseHttpClientFactory(httpFactory, "blueriiot", settings =>
                        {
                            if (config.ServerUrl != null)
                                settings.ServerUri = config.ServerUrl;
                        });
                });

            services
                .AddAllFeatureUpdaters()
                .AddSingleton<FeatureUpdateManager>()
                .AddSingleton<SingleBlueRiiotPoolUpdaterFactory>()
                .AddSingleton<BlueRiiotMqttService>()
                .AddHostedService(x => x.GetRequiredService<BlueRiiotMqttService>());
        }
    }
}
