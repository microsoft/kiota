using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Kiota.Builder.Logging;

public class AggregateLogger<T> : ILogger<T>
{
    private readonly ILogger<T>[] Loggers;
    public AggregateLogger(params ILogger<T>[] loggers)
    {
        Loggers = loggers;
    }
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return new AggregateScope(Loggers.Select(l => l.BeginScope(state)).Where(static s => s != null).Select(static x => x!).ToArray());
    }
    public bool IsEnabled(LogLevel logLevel)
    {
        return Loggers.Any(l => l.IsEnabled(logLevel));
    }
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        foreach (var logger in Loggers)
            logger.Log(logLevel, eventId, state, exception, formatter);
    }
}
