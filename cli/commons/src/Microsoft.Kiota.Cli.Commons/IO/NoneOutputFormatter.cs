using System.CommandLine;
using System.Text.Json;
using Spectre.Console;

namespace Microsoft.Kiota.Cli.Commons.IO;

/// <summary>
/// A no-op output formatter
/// </summary>
public class NoneOutputFormatter : IOutputFormatter
{
    /// <inheritdoc />
    public Task WriteOutputAsync(Stream? content, IOutputFormatterOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
