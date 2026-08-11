using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using System.Threading.Tasks;
using kiota;
using Moq;
using Xunit;

namespace Kiota.Tests;

public sealed class KiotaHostTests : IDisposable
{
    private readonly IConsole _console;
    private readonly List<IDisposable> _disposables = [];
    public KiotaHostTests()
    {
        var consoleMock = new Mock<IConsole>();
        var mockStandardStreamWriter = new Mock<IStandardStreamWriter>();
        var mockWriter = new StringWriter();
        _disposables.Add(mockWriter);
        mockStandardStreamWriter.Setup(w => w.Write(It.IsAny<string>())).Callback<string>(mockWriter.Write);
        consoleMock.Setup(c => c.Out).Returns(mockStandardStreamWriter.Object);
        consoleMock.Setup(c => c.Error).Returns(mockStandardStreamWriter.Object);
        consoleMock.Setup(c => c.IsInputRedirected).Returns(true);
        consoleMock.Setup(c => c.IsOutputRedirected).Returns(true);
        consoleMock.Setup(c => c.IsErrorRedirected).Returns(true);
        _console = consoleMock.Object;
    }
    [Fact]
    public async Task ThrowsOnInvalidOutputPathAsync()
    {
        Assert.Equal(1, await KiotaHost.GetRootCommand().InvokeAsync(["generate", "-o", "A:\\doesnotexist"], _console));
    }
    [Fact]
    public async Task ThrowsOnInvalidInputPathAsync()
    {
        Assert.Equal(1, await KiotaHost.GetRootCommand().InvokeAsync(["generate", "-d", "A:\\doesnotexist"], _console));
    }
    [Fact]
    public async Task ThrowsOnInvalidInputUrlAsync()
    {
        Assert.Equal(1, await KiotaHost.GetRootCommand().InvokeAsync(["generate", "-d", "https://nonexistentdomain56a535ba-bda6-405e-b5e2-ef5f11bf1003.net/doesnotexist"], _console));
    }
    [Fact]
    public async Task ThrowsOnInvalidLanguageAsync()
    {
        Assert.Equal(1, await KiotaHost.GetRootCommand().InvokeAsync(["generate", "-l", "Pascal"], _console));
    }
    [Fact]
    public async Task ThrowsOnInvalidLogLevelAsync()
    {
        Assert.Equal(1, await KiotaHost.GetRootCommand().InvokeAsync(["generate", "--ll", "Dangerous"], _console));
    }
    [Fact]
    public async Task ThrowsOnInvalidClassNameAsync()
    {
        Assert.Equal(1, await KiotaHost.GetRootCommand().InvokeAsync(["generate", "-c", ".Graph"], _console));
        Assert.Equal(1, await KiotaHost.GetRootCommand().InvokeAsync(["generate", "-c", "Graph-api"], _console));
        Assert.Equal(1, await KiotaHost.GetRootCommand().InvokeAsync(["generate", "-c", "1Graph"], _console));
        Assert.Equal(1, await KiotaHost.GetRootCommand().InvokeAsync(["generate", "-c", "Gr@ph"], _console));
    }
    [Fact]
    public async Task AcceptsDeserializersAsync()
    {
        Assert.Equal(1, await KiotaHost.GetRootCommand().InvokeAsync(["generate", "--ds", "Kiota.Tests.TestData.TestDeserializer"], _console));
    }
    [Fact]
    public async Task AcceptsSerializersAsync()
    {
        Assert.Equal(1, await KiotaHost.GetRootCommand().InvokeAsync(["generate", "-s", "Kiota.Tests.TestData.TestSerializer"], _console));
    }
    [Fact]
    public async Task ThrowsOnInvalidSearchTermAsync()
    {
        Assert.Equal(1, await KiotaHost.GetRootCommand().InvokeAsync(["search"], _console));
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables)
            disposable.Dispose();
        GC.SuppressFinalize(this);
    }
}
