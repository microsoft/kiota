using Azure.Monitor.OpenTelemetry.Exporter;
using kiota.Telemetry;
using kiota.Telemetry.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace kiota.Extension;

internal static class KiotaHostExtensions
{
    private const string TelemetryOptOutKey = "KIOTA_CLI_TELEMETRY_OPTOUT";

    internal static IHostBuilder ConfigureKiotaTelemetryServices(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices(ConfigureServiceContainer);

        static void ConfigureServiceContainer(HostBuilderContext context, IServiceCollection services)
        {
            TelemetryConfig cfg = new();
            var section = context.Configuration.GetSection(TelemetryConfig.ConfigSectionKey);
            section.Bind(cfg);
            // Spec mandates using an environment variable for opt-out
            // cfg.Disabled acts as a feature flag, the env var is an option.
            var disabled = Environment.GetEnvironmentVariable(TelemetryOptOutKey)?.ToLowerInvariant();
            if (!cfg.Disabled && disabled is not ("1" or "true"))
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
