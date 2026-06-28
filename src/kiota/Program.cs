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
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // CancellationTokenSource is already disposed when the process exits normally
            }
        };
        await host.StartAsync(cts.Token).ConfigureAwait(false);
        var rootCommand = KiotaHost.GetRootCommand(host.Services);
        var result = await rootCommand.Parse(args).InvokeAsync(null, cts.Token).ConfigureAwait(false);
        DisposeSubCommands(rootCommand);
        await host.StopAsync(cts.Token).ConfigureAwait(false);
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
