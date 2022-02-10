using System.CommandLine;

namespace Microsoft.Kiota.Cli.Commons.IO;

public interface IOutputFormatter
{
    void WriteOutput(string content, IConsole console);

    void WriteOutput(Stream content, IConsole console);
}
