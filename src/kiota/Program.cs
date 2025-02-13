using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using kiota.Extension;
using Microsoft.Extensions.Hosting;

namespace kiota;

static class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = KiotaHost.GetRootCommand();
        var builder = new CommandLineBuilder(rootCommand);
        builder.UseHost(static args =>
        {
            var hostBuilder = Host.CreateDefaultBuilder(args);
            hostBuilder.ConfigureKiotaServices();
            return hostBuilder;
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
}
