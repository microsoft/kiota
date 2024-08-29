using System;
using System.IO;
using Kiota.Builder.Logging;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Kiota.Builder.Tests.Logging;

public sealed class FileLoggerTests : IDisposable
{
    private readonly string _logDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    public FileLoggerTests()
    {
        Directory.CreateDirectory(_logDirectory);
    }
    [Fact]
    public void CleansUpFileWhenNoLogs()
    {
        using (var logger = new FileLogLogger(_logDirectory, LogLevel.Warning, "test"))
        { //using this format intentionally to ensure the dispose is called before the assert
            logger.LogInformation("test");
        }
        Assert.False(File.Exists(Path.Combine(_logDirectory, FileLogLogger.LogFileName)));
    }
    [Fact]
    public void KeepsLogFileWhenLogs()
    {
        using (var logger = new FileLogLogger(_logDirectory, LogLevel.Warning, "test"))
        { //using this format intentionally to ensure the dispose is called before the assert
            logger.LogWarning("test");
        }
        Assert.True(File.Exists(Path.Combine(_logDirectory, FileLogLogger.LogFileName)));
    }

    public void Dispose()
    {
        if (Directory.Exists(_logDirectory))
            Directory.Delete(_logDirectory, true);
    }
}
