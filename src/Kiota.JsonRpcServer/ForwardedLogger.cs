using Microsoft.Extensions.Logging;

namespace Kiota.JsonRpcServer;
public class ForwardedLogger<T> : ILogger<T>
{
    public List<LogEntry> LogEntries { get; private set; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        LogEntries.Add(new(logLevel, formatter(state, exception)));
    }
}
