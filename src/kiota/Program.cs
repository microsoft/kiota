using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using kiota.Extension;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace kiota;

static class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = KiotaHost.GetRootCommand();
        var parser = new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .UseHost(static args =>
            {
                return Host.CreateDefaultBuilder(args)
                    .ConfigureKiotaTelemetryServices()
                    .ConfigureLogging(static logging =>
                    {
                        logging.ClearProviders();
#if DEBUG
                        logging.AddDebug();
#endif
                        logging.AddEventSourceLogger();
                    });
            })
            .Build();
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
}
