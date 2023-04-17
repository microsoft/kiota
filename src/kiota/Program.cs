using System;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;

namespace kiota;
static class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = KiotaHost.GetRootCommand();
        var result = await rootCommand.InvokeAsync(args);
        foreach (var subCommand in rootCommand.Subcommands.Select(static x => x.Handler).OfType<IDisposable>())
            subCommand.Dispose();
        return result;
    }
}
