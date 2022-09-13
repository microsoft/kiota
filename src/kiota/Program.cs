using System.CommandLine;
using System.Threading.Tasks;

namespace kiota
{
    static class Program
    {
        static Task<int> Main(string[] args)
        {
            return new KiotaHost().GetRootCommand().InvokeAsync(args);
        }
    }
}
