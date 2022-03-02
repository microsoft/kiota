namespace Microsoft.Kiota.Cli.Commons.IO;

/// <summary>
/// Factory that provides an output formatter based on a <see cref="FormatterType"/>
/// </summary>
public interface IOutputFormatterFactory
{
    /// <summary>
    /// Returns a formatter that writes content in the provided <see cref="FormatterType"/>
    /// </summary>
    /// <param name="formatterType">The desired formatter type</param>
    /// <returns></returns>
    IOutputFormatter GetFormatter(FormatterType formatterType);
}
