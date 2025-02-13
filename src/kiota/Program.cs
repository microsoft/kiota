using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using Azure.Monitor.OpenTelemetry.Exporter;
using kiota.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace kiota;

static class Program
{
    // TODO: Use config
    const string appInsightsConnectionString = "InstrumentationKey=aacd0827-313f-4e2e-8690-9be8740e3423;IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/;LiveEndpoint=https://eastus.livediagnostics.monitor.azure.com/;ApplicationId=eebbb820-b386-412f-92ee-ae7ac854ed6c";
    static async Task<int> Main(string[] args)
    {
        var rootCommand = KiotaHost.GetRootCommand();
        var builder = new CommandLineBuilder(rootCommand);
        builder.UseHost(static args =>
        {
            var hostBuilder = Host.CreateDefaultBuilder(args);
            hostBuilder.ConfigureServices(ConfigureServiceContainer);
            return hostBuilder;
        });
        var parser = builder.Build();
        var result = await parser.InvokeAsync(args);
        DisposeSubCommands(rootCommand);
        return result;
    }

    private static void ConfigureServiceContainer(IServiceCollection services)
    {
        services.AddOpenTelemetry()
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
            })
            .WithMetrics(static mp =>
            {
                mp.AddMeter($"{TelemetryLabels.ScopeName}*")
                    .AddHttpClientInstrumentation()
                    // TODO: Decide if runtime metrics are useful
                    .AddRuntimeInstrumentation()
                    .SetExemplarFilter(ExemplarFilterType.TraceBased);
                mp.AddOtlpExporter(ConfigureOpenTelemetryProtocolExporter);
                // mp.AddConsoleExporter();
                // mp.AddAzureMonitorMetricExporter(ConfigureAzureMonitorExporter);
            })
            .WithTracing(static tp =>
            {
                tp.AddSource($"{TelemetryLabels.ScopeName}*")
                    .AddHttpClientInstrumentation();
                tp.AddOtlpExporter(ConfigureOpenTelemetryProtocolExporter);
                // tp.AddConsoleExporter();
                // tp.AddAzureMonitorTraceExporter(ConfigureAzureMonitorExporter);
            });
        services.AddSingleton<Instrumentation>();
    }

    private static void DisposeSubCommands(this Command command)
    {
        if (command.Handler is IDisposable disposableHandler)
            disposableHandler.Dispose();
        foreach (var subCommand in command.Subcommands)
        {
            DisposeSubCommands(subCommand);
        }
    }

    private static void ConfigureOpenTelemetryProtocolExporter(OtlpExporterOptions options)
    {
        // TODO: Use config
        options.Endpoint = new Uri("http://localhost:4317");
    }

    private static void ConfigureAzureMonitorExporter(AzureMonitorExporterOptions options)
    {
        options.ConnectionString = appInsightsConnectionString;
    }

    private static string? OsName()
    {
        if (OperatingSystem.IsWindows()) return "windows";
        if (OperatingSystem.IsLinux()) return "linux";
        if (OperatingSystem.IsMacOS()) return "macos";

        return OperatingSystem.IsFreeBSD() ? "freebsd" : null;
    }
}
