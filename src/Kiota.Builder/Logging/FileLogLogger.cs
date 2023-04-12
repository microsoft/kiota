using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Kiota.Builder.Logging;
public class FileLogLogger : ILogger, IDisposable
{
    private readonly string _logFileAbsolutePath = string.Empty;
    private readonly StreamWriter _logStream;
    private readonly LogLevel _logLevel;
    private readonly string _categoryName;
    private const string LogFileName = ".kiota.log";
    public FileLogLogger(string logFileDirectoryAbsolutePath, LogLevel logLevel, string categoryName)
    {
        _logLevel = logLevel;
        _categoryName = categoryName.Split(new char[] { '.', ' ' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;
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
        _logStream.Flush();
        _logStream.Dispose();
        if (!wroteAnything && !string.IsNullOrEmpty(_logFileAbsolutePath) && File.Exists(_logFileAbsolutePath))
            File.Delete(_logFileAbsolutePath);
        GC.SuppressFinalize(this);
    }
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None && logLevel >= _logLevel;
    }
    private bool wroteAnything;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        if (!wroteAnything)
            wroteAnything = true;
        _logStream.WriteLine($"{logLevel}: {_categoryName} {formatter(state, exception)}");
    }
}

public class FileLogLogger<T> : FileLogLogger, ILogger<T>
{
    public FileLogLogger(string logFileDirectoryAbsolutePath, LogLevel logLevel) : base(logFileDirectoryAbsolutePath, logLevel, typeof(T).FullName ?? string.Empty)
    {
    }
}
