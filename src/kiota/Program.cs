using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Azure.Monitor.OpenTelemetry.Exporter;
using kiota.Telemetry;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace kiota;

static class Program
{
    const string appInsightsConnectionString = "InstrumentationKey=88938a3d-6016-4d17-85f7-8dd00afc39d4;IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/;LiveEndpoint=https://eastus.livediagnostics.monitor.azure.com/;ApplicationId=9b0946d3-0df5-4563-9aed-ed6f6fa86fe9";
    static async Task<int> Main(string[] args)
    {
        // TODO: Abstract into telemetry component.
        var version = Kiota.Generated.KiotaVersion.Current();
        var resourceBuilder =
            ResourceBuilder
                .CreateDefault()
                .AddService(serviceName: "kiota", serviceNamespace: "microsoft.openapi", serviceVersion: version);
        if (OsName() is { } osName)
        {
            resourceBuilder.AddAttributes(new[]
            {
                new KeyValuePair<string, object>("os.name", osName),
                new KeyValuePair<string, object>("os.version", Environment.OSVersion.Version.ToString()),
            });
        }
        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter("microsoft.openapi.kiota")
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .SetResourceBuilder(resourceBuilder)
            .SetExemplarFilter(ExemplarFilterType.TraceBased)
            .AddOtlpExporter(ConfigureOpenTelemetryProtocolExporter)
            // .AddAzureMonitorMetricExporter(ConfigureAzureMonitorExporter)
            .Build();
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource("microsoft.openapi.kiota")
            .AddHttpClientInstrumentation()
            .SetResourceBuilder(resourceBuilder)
            .AddOtlpExporter(ConfigureOpenTelemetryProtocolExporter)
            // .AddAzureMonitorTraceExporter(ConfigureAzureMonitorExporter)
            .Build();
        var rootCommand = KiotaHost.GetRootCommand();
        var builder = new CommandLineBuilder(rootCommand);
        using var tc = new TelemetryComponents();
        builder.AddMiddleware(ic =>
        {
            ic.BindingContext.AddService(_ => tc);
        });
        var parser = builder.Build();
        var result = await parser.InvokeAsync(args);
        DisposeSubCommands(rootCommand);
        return result;
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
        // TODO: Update endpoint
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
