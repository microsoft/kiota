using System.CommandLine;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace kiota;

static class Program
{
    static async Task<int> Main(string[] args)
    {
        // TODO: Abstract into telemetry component.
        var version = KiotaVersion.Current();
        var resourceBuilder =
            ResourceBuilder
                .CreateDefault()
                .AddService(serviceName: "kiota", serviceNamespace: "Microsoft.OpenApi", serviceVersion: version);
        Action<OtlpExporterOptions> configureOtlp = options =>
        {
            // TODO: Update endpoint
            options.Endpoint = new Uri("https://localhost:18889");
        };
        // TODO: remove console exporter
        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddHttpClientInstrumentation()
            .AddConsoleExporter()
            .AddOtlpExporter(configureOtlp)
            .Build();
        using var tracerProvider = Sdk.CreateTracerProviderBuilder().SetResourceBuilder(resourceBuilder)
            .AddHttpClientInstrumentation()
            .AddConsoleExporter()
            .AddOtlpExporter(configureOtlp)
            .Build();
        var rootCommand = KiotaHost.GetRootCommand();
        var result = await rootCommand.InvokeAsync(args);
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
}
