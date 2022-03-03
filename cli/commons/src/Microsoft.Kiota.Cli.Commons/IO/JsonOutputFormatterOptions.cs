namespace Microsoft.Kiota.Cli.Commons.IO;

/// <summary>
/// The JSON output formatter options
/// </summary>
public class JsonOutputFormatterOptions : OutputFormatterOptions
{
    /// <summary>
    /// Gets or sets a value that defines whether JSON should use pretty printing. By
    /// default, JSON is serialized without any extra white space.
    /// </summary>
    public bool OutputIndented { get; set; } = true
}
