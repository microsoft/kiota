using System.Text;

namespace Kiota.Web;
public class StringBuilderLogger<T> : ILogger<T>
{
    private readonly StringBuilder _stringBuilder;
    private readonly LogLevel _logLevel;
    private readonly ILogger<T> _logger;
    public StringBuilderLogger(ILogger<T> logger, StringBuilder stringBuilder, LogLevel logLevel)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(stringBuilder);
        _logger = logger;
        _stringBuilder = stringBuilder;
        _logLevel = logLevel;
    }
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _logger.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if(logLevel >= _logLevel)
            _stringBuilder.AppendLine(formatter(state, exception));
        _logger.Log(logLevel, eventId, state, exception, formatter);
    }
}
