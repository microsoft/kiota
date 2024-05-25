using System;
using System.Collections.Generic;
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
        var scopes = new List<IDisposable>();
        foreach (var logger in Loggers)
        {
            var scope = logger.BeginScope(state);
            if (scope != null)
            {
                scopes.Add(scope);
            }
        }
        return new AggregateScope(scopes.ToArray());
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        foreach (var logger in Loggers)
        {
            if (logger.IsEnabled(logLevel))
            {
                return true;
            }
        }
        return false;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        foreach (var logger in Loggers)
            logger.Log(logLevel, eventId, state, exception, formatter);
    }
}
