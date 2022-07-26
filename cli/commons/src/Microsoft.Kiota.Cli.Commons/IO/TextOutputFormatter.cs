using System.CommandLine;
using System.Text.Json;
using Spectre.Console;

namespace Microsoft.Kiota.Cli.Commons.IO;

/// <summary>
/// The JSON output formatter
/// </summary>
public class TextOutputFormatter : IOutputFormatter
{
    private readonly IAnsiConsole _ansiConsole;

    /// <summary>
    /// Creates a new JSON output formatter with a default console
    /// </summary>
    public TextOutputFormatter() : this(AnsiConsole.Console)
    {
    }

    /// <summary>
    /// Creates a new JSON output formatter with the provided console
    /// </summary>
    /// <param name="console">The console to use</param>
    public TextOutputFormatter(IAnsiConsole console)
    {
        this._ansiConsole = console;
    }

    /// <inheritdoc />
    public async Task WriteOutputAsync(Stream content, IOutputFormatterOptions? options = null, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(content);
        const int BUFFER_LENGTH = 4096;
        var charsReceived = 0;
        do {
            var buffer = new char[BUFFER_LENGTH];
            charsReceived = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (charsReceived == 0) {
                break;
            }
            _ansiConsole.Write(new string(buffer, 0, charsReceived));
        } while(charsReceived == BUFFER_LENGTH);
        _ansiConsole.WriteLine();
    }
}
