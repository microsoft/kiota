using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace Kiota
{
    static class Program
    {
        static Task<int> Main(string[] args)
        {
            return KiotaHost.GetRootCommand().InvokeAsync(args);
        }
    }
}
