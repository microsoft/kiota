namespace Microsoft.Kiota.Cli.Commons.IO;

/// <summary>
/// The JSON output formatter options
/// </summary>
public interface IJsonOutputFormatterOptions : IOutputFormatterOptions
{
    /// <summary>
    /// Gets or initializes a value that defines whether JSON should use pretty printing. By
    /// default, JSON is serialized without any extra white space.
    /// </summary>
    public bool OutputIndented { get; init; }
}
