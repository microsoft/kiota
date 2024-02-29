using System;
using System.IO;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.WorkspaceManagement;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests.WorkspaceManagement;

public sealed class WorkspaceManagementServiceTests : IDisposable
{
    private readonly string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    [Fact]
    public void Defensive()
    {
        Assert.Throws<ArgumentNullException>(() => new WorkspaceManagementService(null));
    }
    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public async Task IsClientPresentReturnsFalseOnNoClient(bool usesConfig)
    {
        var mockLogger = new Mock<ILogger>();
        var service = new WorkspaceManagementService(mockLogger.Object, usesConfig);
        var result = await service.IsClientPresent("clientName");
        Assert.False(result);
    }
    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public async Task ShouldGenerateReturnsTrue(bool usesConfig)
    {
        var mockLogger = new Mock<ILogger>();
        var service = new WorkspaceManagementService(mockLogger.Object, usesConfig);
        var configuration = new GenerationConfiguration
        {
            ClientClassName = "clientName",
            OutputPath = tempPath,
            OpenAPIFilePath = Path.Combine(tempPath, "openapi.yaml"),
        };
        var result = await service.ShouldGenerateAsync(configuration, "foo");
        Assert.True(result);
    }
    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public async Task ShouldGenerateReturnsFalse(bool usesConfig)
    {
        var mockLogger = new Mock<ILogger>();
        var service = new WorkspaceManagementService(mockLogger.Object, usesConfig);
        var configuration = new GenerationConfiguration
        {
            ClientClassName = "clientName",
            OutputPath = tempPath,
            OpenAPIFilePath = Path.Combine(tempPath, "openapi.yaml"),
            ApiRootUrl = "https://graph.microsoft.com",
        };
        Directory.CreateDirectory(tempPath);
        await service.UpdateStateFromConfigurationAsync(configuration, "foo", []);
        var result = await service.ShouldGenerateAsync(configuration, "foo");
        Assert.False(result);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempPath))
            Directory.Delete(tempPath, true);
    }
}
