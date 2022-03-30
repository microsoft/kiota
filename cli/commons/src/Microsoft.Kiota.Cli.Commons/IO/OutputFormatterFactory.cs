namespace Microsoft.Kiota.Cli.Commons.IO;

/// <inheritdoc />
public sealed class OutputFormatterFactory : IOutputFormatterFactory
{
    /// <inheritdoc />
    public IOutputFormatter GetFormatter(FormatterType formatterType)
    {
        return formatterType switch
        {
            FormatterType.JSON => new JsonOutputFormatter(),
            FormatterType.TABLE => new TableOutputFormatter(),
            FormatterType.TEXT => new TextOutputFormatter(),
            _ => throw new NotSupportedException(),
        };
    }

    /// <inheritdoc />
    public IOutputFormatter GetFormatter(string format)
    {
        var success = Enum.TryParse(format, true, out FormatterType type);
        if (!success)
        {
            throw new NotSupportedException();
        }
        return GetFormatter(type);
    }
}
