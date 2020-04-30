using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MBW.BlueRiiot2MQTT.Configuration;
using MBW.BlueRiiot2MQTT.Features;
using MBW.BlueRiiot2MQTT.HASS;
using MBW.BlueRiiot2MQTT.Helpers;
using MBW.BlueRiiot2MQTT.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using Polly;
using Serilog;
using Serilog.Extensions.Logging;
using WebProxy = System.Net.WebProxy;

namespace MBW.BlueRiiot2MQTT
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ssK} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}")
                .CreateLogger();

            await CreateHostBuilder(args).RunConsoleAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(builder => builder.AddJsonFile("appsettings.local.json", true))
                .ConfigureLogging(builder => builder
                    .ClearProviders()
                    .AddSerilog()
                    .AddFilter<SerilogLoggerProvider>("System.Net.Http.HttpClient.blueriiot.LogicalHandler", LogLevel.Warning)
                    .AddFilter<SerilogLoggerProvider>("Microsoft.Extensions.Http", LogLevel.Warning))
                .ConfigureServices(ConfigureServices);

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services
                .Configure<MqttConfiguration>(context.Configuration.GetSection("MQTT"))
                .AddSingleton<IMqttFactory>(provider =>
                {
                    ILoggerFactory loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                    ExtensionsLoggingMqttLogger logger = new ExtensionsLoggingMqttLogger(loggerFactory, "MqttNet");

                    return new MqttFactory(logger);
                })
                .AddSingleton<HassWillContainer>()
                .AddHostedService<HassAliveAndWillService>()
                .AddHostedService<MqttConnectionService>()
                .AddSingleton<MqttEvents>()
                .AddSingleton<IMqttClientOptions>(provider =>
                {
                    HassWillContainer willContainer = provider.GetRequiredService<HassWillContainer>();
                    MqttConfiguration mqttConfig = provider.GetOptions<MqttConfiguration>();

                    // Prepare options
                    MqttClientOptionsBuilder optionsBuilder = new MqttClientOptionsBuilder()
                        .WithTcpServer(mqttConfig.Server, mqttConfig.Port)
                        .WithCleanSession(false)
                        .WithClientId(mqttConfig.ClientId)
                        .WithWillMessage(new MqttApplicationMessage
                        {
                            Topic = willContainer.StateTopic,
                            Payload = Encoding.UTF8.GetBytes(HassWillContainer.NotRunningMessage),
                            Retain = true
                        })
                        .WithCommunicationTimeout(TimeSpan.FromSeconds(10));

                    if (!string.IsNullOrEmpty(mqttConfig.Username))
                        optionsBuilder.WithCredentials(mqttConfig.Username, mqttConfig.Password);

                    return optionsBuilder.Build();
                })
                .AddSingleton<IMqttClient>(provider =>
                {
                    IHostApplicationLifetime appLifetime = provider.GetRequiredService<IHostApplicationLifetime>();
                    CancellationToken stoppingtoken = appLifetime.ApplicationStopping;

                    MqttEvents mqttEvents = provider.GetRequiredService<MqttEvents>();

                    // TODO: Support TLS & client certs
                    IMqttFactory factory = provider.GetRequiredService<IMqttFactory>();

                    // Prepare options
                    IMqttClientOptions options = provider.GetRequiredService<IMqttClientOptions>();

                    // Create client
                    IMqttClient mqttClient = factory.CreateMqttClient();

                    // Hook up event handlers
                    mqttClient.UseDisconnectedHandler(async args =>
                    {
                        await mqttEvents.InvokeDisconnectHandler(args, stoppingtoken);
                    });
                    mqttClient.UseConnectedHandler(async args =>
                    {
                        await mqttEvents.InvokeConnectHandler(args, stoppingtoken);
                    });

                    // Connect
                    mqttClient.ConnectAsync(options, stoppingtoken);

                    return mqttClient;
                });

            services
                .Configure<HassConfiguration>(context.Configuration.GetSection("HASS"))
                .Configure<BlueRiiotConfiguration>(context.Configuration.GetSection("BlueRiiot"))
                .Configure<ProxyConfiguration>(context.Configuration.GetSection("Proxy"))
                .AddSingleton<HassTopicBuilder>()
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

                    if (proxyConfig.UseProxy)
                        handler.Proxy = new WebProxy(proxyConfig.ProxyUri);

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
                .AddSingleton<SensorStore>()
                .AddAllFeatureUpdaters()
                .AddSingleton<FeatureUpdateManager>()
                .AddHostedService<BlueRiiotMqttService>();
        }
    }
}
