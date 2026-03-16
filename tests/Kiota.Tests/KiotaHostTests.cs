using System;
using System.Threading.Tasks;
using kiota;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kiota.Tests;

public sealed class KiotaHostTests
{
    private static IServiceProvider EmptyServiceProvider => new ServiceCollection().BuildServiceProvider();
    [Fact]
    public async Task ThrowsOnInvalidOutputPathAsync()
    {
        Assert.Equal(1, await KiotaHost.GetRootCommand(EmptyServiceProvider).Parse(["generate", "-o", "A:\\doesnotexist"]).InvokeAsync());
    }
    [Fact]
    public async Task ThrowsOnInvalidInputPathAsync()
    {
        Assert.Equal(1, await KiotaHost.GetRootCommand(EmptyServiceProvider).Parse(["generate", "-d", "A:\\doesnotexist"]).InvokeAsync());
    }
    [Fact]
    public async Task ThrowsOnInvalidInputUrlAsync()
    {
        Assert.Equal(1, await KiotaHost.GetRootCommand(EmptyServiceProvider).Parse(["generate", "-d", "https://nonexistentdomain56a535ba-bda6-405e-b5e2-ef5f11bf1003.net/doesnotexist"]).InvokeAsync());
    }
    [Fact]
    public async Task ThrowsOnInvalidLanguageAsync()
    {
        Assert.Equal(1, await KiotaHost.GetRootCommand(EmptyServiceProvider).Parse(["generate", "-l", "Pascal"]).InvokeAsync());
    }
    [Fact]
    public async Task ThrowsOnInvalidLogLevelAsync()
    {
        Assert.Equal(1, await KiotaHost.GetRootCommand(EmptyServiceProvider).Parse(["generate", "--ll", "Dangerous"]).InvokeAsync());
    }
    [Fact]
    public async Task ThrowsOnInvalidClassNameAsync()
    {
        Assert.Equal(1, await KiotaHost.GetRootCommand(EmptyServiceProvider).Parse(["generate", "-c", ".Graph"]).InvokeAsync());
        Assert.Equal(1, await KiotaHost.GetRootCommand(EmptyServiceProvider).Parse(["generate", "-c", "Graph-api"]).InvokeAsync());
        Assert.Equal(1, await KiotaHost.GetRootCommand(EmptyServiceProvider).Parse(["generate", "-c", "1Graph"]).InvokeAsync());
        Assert.Equal(1, await KiotaHost.GetRootCommand(EmptyServiceProvider).Parse(["generate", "-c", "Gr@ph"]).InvokeAsync());
    }
    [Fact]
    public async Task AcceptsDeserializersAsync()
    {
        Assert.Equal(1, await KiotaHost.GetRootCommand(EmptyServiceProvider).Parse(["generate", "--ds", "Kiota.Tests.TestData.TestDeserializer"]).InvokeAsync());
    }
    [Fact]
    public async Task AcceptsSerializersAsync()
    {
        Assert.Equal(1, await KiotaHost.GetRootCommand(EmptyServiceProvider).Parse(["generate", "-s", "Kiota.Tests.TestData.TestSerializer"]).InvokeAsync());
    }
    [Fact]
    public async Task ThrowsOnInvalidSearchTermAsync()
    {
        Assert.Equal(1, await KiotaHost.GetRootCommand(EmptyServiceProvider).Parse(["search"]).InvokeAsync());
    }
}
