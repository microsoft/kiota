using System.CommandLine;

namespace Microsoft.Kiota.Cli.Commons.IO;

public interface IOutputFormatter
{
    void WriteOutput(string content);

    void WriteOutput(Stream content);
}
