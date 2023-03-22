using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Kiota.Builder.Tests;

internal class CountLogger<T> : ILogger<T>
{
    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        throw new NotImplementedException();
    }
    public bool IsEnabled(LogLevel logLevel)
    {
        throw new NotImplementedException();
    }
    internal Dictionary<LogLevel, int> Count { get; } = new();
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (Count.ContainsKey(logLevel))
            Count[logLevel] += 1;
        else
            Count.Add(logLevel, 1);
    }
}
