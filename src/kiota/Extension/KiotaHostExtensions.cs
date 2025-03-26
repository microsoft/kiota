using System.Runtime.CompilerServices;
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

[assembly: InternalsVisibleTo("Kiota.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100957cb48387b2a5f54f5ce39255f18f26d32a39990db27cf48737afc6bc62759ba996b8a2bfb675d4e39f3d06ecb55a178b1b4031dcb2a767e29977d88cce864a0d16bfc1b3bebb0edf9fe285f10fffc0a85f93d664fa05af07faa3aad2e545182dbf787e3fd32b56aca95df1a3c4e75dec164a3f1a4c653d971b01ffc39eb3c4")]

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
                                new KeyValuePair<string, object>("os.type", osName),
                                new KeyValuePair<string, object>("os.version", Environment.OSVersion.Version.ToString()),
                            ]);
                            r.AddAttributes([
                                new KeyValuePair<string, object>(TelemetryLabels.TagAcquisitionChannel, AcquisitionChannel(Environment.ProcessPath)),
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

    internal static string AcquisitionChannel(string? path)
    {
        // Docker
        if (Environment.GetEnvironmentVariable("KIOTA_CONTAINER") == "true") return "docker";
        if (path != null && !string.IsNullOrWhiteSpace(path))
        {

            var absolutePath = Path.GetFullPath(path);
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            // Dotnet tool
            if (absolutePath.StartsWith(Path.Join(homeDir, ".dotnet", "tools")))
            {
                return "dotnet_tool";
            }
            // Extension
            if (absolutePath.Contains(".vscode"))
            {
                return "extension";
            }

            // No reliable way to check this at compile time (especially for dotnet tool)
            if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
            {
                // ASDF
                // https://asdf-vm.com/manage/configuration.html#asdf-data-dir
                var asdfDataDir = Environment.GetEnvironmentVariable("ASDF_DATA_DIR") ?? Path.Join(homeDir, ".asdf");
                if (absolutePath.StartsWith(asdfDataDir))
                {
                    return "asdf";
                }

                // https://docs.brew.sh/Formula-Cookbook#variables-for-directory-locations
                var homebrewRoot = Environment.GetEnvironmentVariable("HOMEBREW_PREFIX") ?? "/opt/homebrew";
                var dir = Path.Join(homebrewRoot, "Cellar/kiota");
                // Homebrew
                if (absolutePath.StartsWith(dir))
                {
                    return "homebrew";
                }
            }
        }
        return "unknown";
    }
}
