using System.CommandLine;
using System.Text;
using System.Text.Json;
using Spectre.Console;

namespace Microsoft.Kiota.Cli.Commons.IO;

/// <summary>
/// The JSON output formatter
/// </summary>
public class JsonOutputFormatter : IOutputFormatter
{
    /// <inheritdoc />
    public void WriteOutput(string content, IOutputFormatterOptions options)
    {
        if (options is JsonOutputFormatterOptions jsonOptions && jsonOptions.OutputIndented)
        {
            var result = ProcessJson(content, jsonOptions.OutputIndented);
            AnsiConsole.WriteLine(result);
        }
        else
        {
            AnsiConsole.WriteLine(content);
        }
    }

    /// <inheritdoc />
    public void WriteOutput(Stream content, IOutputFormatterOptions options)
    {
        using var reader = new StreamReader(content);
        var strContent = reader.ReadToEnd();
        if (options is JsonOutputFormatterOptions jsonOptions && jsonOptions.OutputIndented)
        {
            var result = ProcessJson(strContent, jsonOptions.OutputIndented);
            AnsiConsole.WriteLine(result);
        }
        else
        {
            AnsiConsole.WriteLine(strContent);
        }
    }

    /// <summary>
    /// Given a JSON input string, returns a processed JSON string with optional indentation
    /// </summary>
    /// <param name="input">JSON input string</param>
    /// <param name="indent">Whether to return indented output</param>
    private static string ProcessJson(string input, bool indent = true)
    {
        var result = input;
        try
        {
            var jsonDoc = JsonDocument.Parse(input);
            result = JsonSerializer.Serialize(jsonDoc, options: new() { WriteIndented = indent });
        }
        catch (JsonException)
        {
        }

        return result;
    }
}
