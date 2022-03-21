namespace Microsoft.Kiota.Cli.Commons.IO;

/// <summary>
/// Extension methods for the formatter type
/// </summary>
public static class FormatterTypeExtensions
{
    /// <summary>
    /// Returns output options for a given formatter type
    /// </summary>
    /// <param name="formatterType">The formatter type to get options for</param>
    /// <param name="options">Options to apply</param>
    public static IOutputFormatterOptions? GetOutputFormatterOptions(this FormatterType formatterType, FormatterOptionsModel? options = null)
    {
        IOutputFormatterOptions? formatterOptions = null;
        if (formatterType == FormatterType.JSON)
        {
            var outputIndented = options?.JsonOutputIndented ?? true;
            formatterOptions = new JsonOutputFormatterOptions(outputIndented);
        }
        else if (formatterType == FormatterType.TABLE)
        {
            formatterOptions = new TableOutputFormatterOptions();
        }

        return formatterOptions;
    }
}
