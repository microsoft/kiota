using System.Text;

namespace Kiota.Web;
public class StringBuilderLogger<T> : ILogger<T>
{
    private readonly StringBuilder _stringBuilder;
    private readonly LogLevel _logLevel;
    public StringBuilderLogger(StringBuilder stringBuilder, LogLevel logLevel)
    {
        ArgumentNullException.ThrowIfNull(stringBuilder);
        _stringBuilder = stringBuilder;
        _logLevel = logLevel;
    }
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && logLevel >= _logLevel;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;
        _stringBuilder.AppendLine(formatter(state, exception));
    }
}
