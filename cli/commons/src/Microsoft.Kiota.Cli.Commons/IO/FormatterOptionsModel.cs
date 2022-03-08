namespace Microsoft.Kiota.Cli.Commons.IO;

/// <summary>
/// Options model for output formatters.
/// </summary>
public readonly struct FormatterOptionsModel {
    /// <summary>
    /// Create a new instance of the model
    /// </summary>
    /// <param name="jsonOutputIndented">Indicates whether to indent JSON output</param>
    public FormatterOptionsModel(bool jsonOutputIndented = true)
    {
        this.JsonOutputIndented = jsonOutputIndented;
    }

    /// <summary>
    /// Property that indicates whether to indent JSON output
    /// </summary>
    public bool JsonOutputIndented { get; init; }

    /// <inheritdoc />
    public override string ToString() => $"({nameof(JsonOutputIndented)} => {JsonOutputIndented})";
}
