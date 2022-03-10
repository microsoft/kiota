namespace Microsoft.Kiota.Cli.Commons.IO;

public sealed class OutputFormatterFactory : IOutputFormatterFactory
{
    public IOutputFormatter GetFormatter(FormatterType formatterType)
    {
        return formatterType switch
        {
            FormatterType.JSON => new JsonOutputFormatter(),
            FormatterType.TABLE => new TableOutputFormatter(),
            _ => throw new NotSupportedException(),
        };
    }

    public IOutputFormatter GetFormatter(string format)
    {
        FormatterType type;
        var success = Enum.TryParse(format, true, out type);
        if (!success)
        {
            throw new NotSupportedException();
        }
        return GetFormatter(type);
    }
}
