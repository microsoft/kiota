using System.CommandLine.Hosting;
using Azure.Monitor.OpenTelemetry.Exporter;
using kiota.Telemetry;
using kiota.Telemetry.Config;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace kiota.Extension;

internal static class KiotaHostExtensions
{
    internal static IHostBuilder ConfigureBaseServices(this IHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(static (ctx, configuration) =>
        {
            var defaultStream = new MemoryStream(Kiota.Generated.KiotaAppSettings.Default());
            configuration.AddJsonStream(defaultStream)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables(prefix: "KIOTA_");
        });
        builder.ConfigureServices(static (ctx, services) =>
        {
            services.Configure<KiotaConfiguration>(ctx.Configuration);
            services.Configure<TelemetryConfig>(ctx.Configuration.GetSection(TelemetryConfig.ConfigSectionKey));
            services.AddHttpClient(string.Empty).ConfigurePrimaryHttpMessageHandler(static sp =>
            {
                var overrides = sp.GetRequiredService<CliOverrides>();
                var httpClientHandler = new HttpClientHandler();
                if (overrides.GetEffectiveDisableSslValidation())
                {
                    httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }
                return httpClientHandler;
            });
            services.AddSingleton<CliOverrides>();
            services.AddKeyedSingleton<GenerationConfiguration>(ServiceConstants.ServiceKeys.Default);
        });
        builder.ConfigureLogging(static (ctx, logging) =>
        {
            logging.ClearProviders();
#if DEBUG
            logging.AddDebug();
#endif
            logging.AddEventSourceLogger();

            // TODO: Add the file logger and find a strategy for changing the output path
            var parseResult = ctx.GetInvocationContext().ParseResult;
            var logLevelResult = parseResult.FindResultFor(KiotaHost.LogLevelOption.Value);
            if (logLevelResult != null)
            {
                logging.SetMinimumLevel(logLevelResult.GetValueOrDefault<LogLevel>());
            }
        });
        return builder;
    }

    internal static IHostBuilder ConfigureKiotaTelemetryServices(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices(ConfigureServiceContainer);

        static void ConfigureServiceContainer(HostBuilderContext context, IServiceCollection services)
        {
            TelemetryConfig cfg = new();
            var section = context.Configuration.GetSection(TelemetryConfig.ConfigSectionKey);
            section.Bind(cfg);
            if (!cfg.Disabled)
            {
                // Only register if telemetry is enabled.
                var openTelemetryBuilder = services.AddOpenTelemetry()
                    .ConfigureResource(static r =>
                    {
                        r.AddService(serviceName: "kiota",
                                serviceNamespace: "microsoft.openapi",
                                serviceVersion: Kiota.Generated.KiotaVersion.Current());
                        if (OsName() is { } osName)
                        {
                            r.AddAttributes([
                                new KeyValuePair<string, object>("os.name", osName),
                                new KeyValuePair<string, object>("os.version", Environment.OSVersion.Version.ToString())
                            ]);
                        }
                    });
                openTelemetryBuilder.WithMetrics(static mp =>
                {
                    mp.AddMeter($"{TelemetryLabels.ScopeName}*")
                        .AddHttpClientInstrumentation()
                        // Decide if runtime metrics are useful
                        .AddRuntimeInstrumentation()
                        .SetExemplarFilter(ExemplarFilterType.TraceBased);
                })
                .WithTracing(static tp =>
                {
                    tp.AddSource($"{TelemetryLabels.ScopeName}*")
                        .AddHttpClientInstrumentation();
                });
                if (cfg.OpenTelemetry.Enabled)
                {
                    // Only register OpenTelemetry exporter if enabled.
                    Action<OtlpExporterOptions> exporterOpts = op =>
                    {
                        if (!string.IsNullOrWhiteSpace(cfg.OpenTelemetry.EndpointAddress))
                        {
                            op.Endpoint = new Uri(cfg.OpenTelemetry.EndpointAddress);
                        }
                    };
                    openTelemetryBuilder
                        .WithMetrics(mp => mp.AddOtlpExporter(exporterOpts))
                        .WithTracing(tp => tp.AddOtlpExporter(exporterOpts));
                }
                if (cfg.AppInsights.Enabled && !string.IsNullOrWhiteSpace(cfg.AppInsights.ConnectionString))
                {
                    // Only register app insights exporter if it's enabled and we have a connection string.
                    Action<AzureMonitorExporterOptions> azureMonitorExporterOptions = options =>
                    {
                        options.ConnectionString = cfg.AppInsights.ConnectionString;
                    };
                    openTelemetryBuilder
                        .WithMetrics(mp => mp.AddAzureMonitorMetricExporter(azureMonitorExporterOptions))
                        .WithTracing(tp => tp.AddAzureMonitorTraceExporter(azureMonitorExporterOptions));
                }
                services.AddSingleton<Instrumentation>();
            }
        }
        static string? OsName()
        {
            if (OperatingSystem.IsWindows()) return "windows";
            if (OperatingSystem.IsLinux()) return "linux";
            if (OperatingSystem.IsMacOS()) return "macos";

            return OperatingSystem.IsFreeBSD() ? "freebsd" : null;
        }
    }
}
