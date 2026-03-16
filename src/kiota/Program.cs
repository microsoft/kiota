using System.CommandLine;
using kiota.Extension;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace kiota;

static class Program
{
    static async Task<int> Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureKiotaTelemetryServices()
            .ConfigureLogging(static logging =>
            {
                logging.ClearProviders();
#if DEBUG
                logging.AddDebug();
#endif
                logging.AddEventSourceLogger();
            })
            .Build();
        await host.StartAsync().ConfigureAwait(false);
        var rootCommand = KiotaHost.GetRootCommand(host.Services);
        var result = await rootCommand.Parse(args).InvokeAsync().ConfigureAwait(false);
        DisposeSubCommands(rootCommand);
        await host.StopAsync().ConfigureAwait(false);
        host.Dispose();
        return result;
    }

    private static void DisposeSubCommands(this Command command)
    {
        if (command.Action is IDisposable disposableHandler)
            disposableHandler.Dispose();
        foreach (var subCommand in command.Subcommands)
        {
            DisposeSubCommands(subCommand);
        }
    }
}
