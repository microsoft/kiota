﻿using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Kiota.Builder.Logging;
public class FileLogLogger : ILogger, IDisposable
{
    private readonly string _logFileAbsolutePath = string.Empty;
    private readonly StreamWriter _logStream;
    private readonly LogLevel _logLevel;
    private readonly string _categoryName;
    internal const string LogFileName = ".kiota.log";
    private readonly object writeLock = new();
    public FileLogLogger(string logFileDirectoryAbsolutePath, LogLevel logLevel, string categoryName)
    {
        _logLevel = logLevel;
        if (categoryName != null)
        {
            var splitCategoryName = categoryName.Split(categoryNameSeparators, StringSplitOptions.RemoveEmptyEntries);
            _categoryName = splitCategoryName.Length > 0 ? splitCategoryName[splitCategoryName.Length - 1] : string.Empty;
        }
        else
        {
            _categoryName = string.Empty;
        }

        if (_logLevel == LogLevel.None || string.IsNullOrEmpty(logFileDirectoryAbsolutePath))
            _logStream = new StreamWriter(Stream.Null);
        else
        {
            var logFileAbsolutePath = Path.Combine(logFileDirectoryAbsolutePath, LogFileName);
            if (File.Exists(logFileAbsolutePath))
                File.Delete(logFileAbsolutePath);
            if (!Directory.Exists(logFileDirectoryAbsolutePath))
                Directory.CreateDirectory(logFileDirectoryAbsolutePath);
            _logStream = new StreamWriter(logFileAbsolutePath);
            _logFileAbsolutePath = logFileAbsolutePath;
        }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
            return;
        _logStream.Flush();
        _logStream.Dispose();
        lock (writeLock)
        {
            if (!wroteAnything && !string.IsNullOrEmpty(_logFileAbsolutePath) && File.Exists(_logFileAbsolutePath))
                File.Delete(_logFileAbsolutePath);
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None && logLevel >= _logLevel;
    }

    private bool wroteAnything;

    private static readonly char[] categoryNameSeparators = ['.', ' '];

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (formatter is null) return;
        if (!IsEnabled(logLevel))
            return;

        lock (writeLock)
        {
            if (!wroteAnything)
                wroteAnything = true;
            _logStream.WriteLine($"{logLevel}: {_categoryName} {formatter(state, exception)}");
        }
    }
}

public class FileLogLogger<T> : FileLogLogger, ILogger<T>
{
    public FileLogLogger(string logFileDirectoryAbsolutePath, LogLevel logLevel) : base(logFileDirectoryAbsolutePath, logLevel, typeof(T).FullName ?? string.Empty)
    {
    }
}
