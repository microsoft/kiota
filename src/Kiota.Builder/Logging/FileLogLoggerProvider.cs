using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Kiota.Builder.Logging;

public sealed class FileLogLoggerProvider : ILoggerProvider
{
    private readonly LogLevel _logLevel;
    private readonly string _logFileDirectoryAbsolutePath;
    public FileLogLoggerProvider(string logFileDirectoryAbsolutePath, LogLevel logLevel)
    {
        _logLevel = logLevel;
        _logFileDirectoryAbsolutePath = logFileDirectoryAbsolutePath;
    }
    private readonly List<FileLogLogger> _loggers = new();
    public ILogger CreateLogger(string categoryName)
    {
        var instance = new FileLogLogger(_logFileDirectoryAbsolutePath, _logLevel, categoryName);
        _loggers.Add(instance);
        return instance;
    }
    public void Dispose()
    {
        foreach (var logger in _loggers)
            logger.Dispose();
        GC.SuppressFinalize(this);
    }
}
