namespace Microsoft.Kiota.Cli.Commons.IO;

public interface IOutputFormatterFactory
{
    IOutputFormatter GetFormatter(FormatterType formatterType);
}
