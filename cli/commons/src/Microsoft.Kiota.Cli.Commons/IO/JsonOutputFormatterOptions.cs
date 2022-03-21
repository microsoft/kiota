namespace Microsoft.Kiota.Cli.Commons.IO;

/// <summary>
/// The JSON output formatter options
/// </summary>
public class JsonOutputFormatterOptions : IJsonOutputFormatterOptions
{
    /// <summary>
    /// Create new instance of JSON output formatter
    /// </summary>
    public JsonOutputFormatterOptions(bool outputIndented)
    {
        this.OutputIndented = outputIndented;
    }

    /// <inheritdoc />
    public bool OutputIndented { get; init; }
}
