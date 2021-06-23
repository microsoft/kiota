using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace Kiota
{
    static class Program
    {
        static Task<int> Main(string[] args)
        {
            return new KiotaHost().GetRootCommand().InvokeAsync(args);
        }
    }
}
