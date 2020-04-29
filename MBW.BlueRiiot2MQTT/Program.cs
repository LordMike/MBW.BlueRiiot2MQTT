using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using MBW.BlueRiiot2MQTT.Configuration;
using MBW.BlueRiiot2MQTT.Features;
using MBW.BlueRiiot2MQTT.HASS;
using MBW.BlueRiiot2MQTT.Helpers;
using MBW.BlueRiiot2MQTT.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Serilog;
using Serilog.Extensions.Logging;
using uPLibrary.Networking.M2Mqtt;

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
                .AddSingleton<MqttClient>(provider =>
                {
                    MqttConfiguration config = provider.GetOptions<MqttConfiguration>();
                    ILogger<MqttClient> logger = provider.GetLogger<MqttClient>();

                    // TODO: Support TLS & client certs
                    MqttClient client = new MqttClient(config.Server, config.Port,
                        false, MqttSslProtocols.SSLv3,
                        (sender, certificate, chain, errors) => false,
                        (sender, host, certificates, certificate, issuers) => null);

                    client.ConnectionClosed += (sender, args) =>
                    {
                        logger.LogWarning("MQTT connection was closed");
                    };

                    // Connect with provided credentials, null values means no user/pass
                    client.Connect(config.ClientId, config.Username, config.Password);

                    // Register disconnect
                    provider.GetRequiredService<IHostApplicationLifetime>()
                        .ApplicationStopping.Register(o => ((MqttClient)o).Disconnect(), client);

                    return client;
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
