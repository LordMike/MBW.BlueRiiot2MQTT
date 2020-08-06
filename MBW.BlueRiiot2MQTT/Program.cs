using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MBW.BlueRiiot2MQTT.Commands;
using MBW.BlueRiiot2MQTT.Configuration;
using MBW.BlueRiiot2MQTT.Features;
using MBW.BlueRiiot2MQTT.Helpers;
using MBW.BlueRiiot2MQTT.Service;
using MBW.HassMQTT;
using MBW.HassMQTT.CommonServices.AliveAndWill;
using MBW.HassMQTT.CommonServices.Commands;
using MBW.HassMQTT.CommonServices.MqttReconnect;
using MBW.HassMQTT.Extensions;
using MBW.HassMQTT.Topics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
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
                .Configure<MqttConfiguration>(context.Configuration.GetSection("MQTT"))
                .AddMqttClientFactoryWithLogging()
                .AddSingleton<IMqttClientOptions>(provider =>
                {
                    MqttConfiguration mqttConfig = provider.GetOptions<MqttConfiguration>();

                    // Prepare options
                    MqttClientOptionsBuilder optionsBuilder = new MqttClientOptionsBuilder()
                        .WithTcpServer(mqttConfig.Server, mqttConfig.Port)
                        .WithCleanSession(false)
                        .WithClientId(mqttConfig.ClientId)
                        .ConfigureHassConnectedEntityServiceLastWill(provider);

                    if (!string.IsNullOrEmpty(mqttConfig.Username))
                        optionsBuilder.WithCredentials(mqttConfig.Username, mqttConfig.Password);

                    if (mqttConfig.KeepAlivePeriod.HasValue)
                        optionsBuilder.WithKeepAlivePeriod(mqttConfig.KeepAlivePeriod.Value);

                    return optionsBuilder.Build();
                })
                .AddSingleton<IMqttClient>(provider =>
                {
                    // TODO: Support TLS & client certs
                    IHostApplicationLifetime appLifetime = provider.GetRequiredService<IHostApplicationLifetime>();
                    CancellationToken stoppingtoken = appLifetime.ApplicationStopping;

                    IMqttFactory factory = provider.GetRequiredService<IMqttFactory>();
                    IMqttClientOptions options = provider.GetRequiredService<IMqttClientOptions>();
                    IMqttClient mqttClient = factory.CreateMqttClient();

                    // Hook up event handlers
                    mqttClient.ConfigureMqttEvents(provider, stoppingtoken);

                    // Connect
                    mqttClient.ConnectAsync(options, stoppingtoken);

                    return mqttClient;
                });

            // MQTT Services
            services
                .AddMqttMessageReceiverService()
                .AddMqttEvents();

            // MQTT Reconnect service
            services
                .AddMqttReconnectService()
                .Configure<MqttReconnectionServiceConfig>(context.Configuration.GetSection("MQTT"));

            // Hass Connected service (MQTT Last Will)
            services
                .AddHassConnectedEntityService("BlueRiiot2MQTT");

            // Hass system services
            services
                .AddSingleton<HassMqttManager>();

            // Commands
            services
                .AddMqttCommandService()
                .AddMqttCommandHandler<ForceSyncCommand>();

            services
                .Configure<HassConfiguration>(context.Configuration.GetSection("HASS"))
                .Configure<BlueRiiotConfiguration>(context.Configuration.GetSection("BlueRiiot"))
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
                        .UseHttpClientFactory(httpFactory, "blueriiot");
                });

            services
                .AddAllFeatureUpdaters()
                .AddSingleton<FeatureUpdateManager>()
                .AddSingleton<BlueRiiotMqttService>()
                .AddHostedService(x => x.GetRequiredService<BlueRiiotMqttService>());
        }
    }
}
