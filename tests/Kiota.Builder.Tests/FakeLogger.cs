using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace kiota.Rpc;

public record FakeLogEntry(
    LogLevel level,
    string message
);

public class FakeLogger<T> : ILogger<T>
{
    public List<FakeLogEntry> LogEntries { get; } = new();

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
