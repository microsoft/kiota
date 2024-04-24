using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Lock;
using Kiota.Builder.Tests.Manifest;
using Kiota.Builder.WorkspaceManagement;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests.WorkspaceManagement;

public sealed class WorkspaceManagementServiceTests : IDisposable
{
    private readonly string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly HttpClient httpClient = new();
    [Fact]
    public void Defensive()
    {
        Assert.Throws<ArgumentNullException>(() => new WorkspaceManagementService(null, httpClient));
        Assert.Throws<ArgumentNullException>(() => new WorkspaceManagementService(Mock.Of<ILogger>(), null));
    }
    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public async Task IsClientPresentReturnsFalseOnNoClient(bool usesConfig)
    {
        var mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(mockLogger, httpClient, usesConfig, tempPath);
        var result = await service.IsConsumerPresent("clientName");
        Assert.False(result);
    }
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [Theory]
    public async Task ShouldGenerateReturnsTrue(bool usesConfig, bool cleanOutput)
    {
        var mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(mockLogger, httpClient, usesConfig, tempPath);
        var configuration = new GenerationConfiguration
        {
            ClientClassName = "clientName",
            OutputPath = tempPath,
            OpenAPIFilePath = Path.Combine(tempPath, "openapi.yaml"),
            CleanOutput = cleanOutput,
        };
        var result = await service.ShouldGenerateAsync(configuration, "foo");
        Assert.True(result);
    }
    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public async Task ShouldGenerateReturnsFalse(bool usesConfig)
    {
        var mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(mockLogger, httpClient, usesConfig, tempPath);
        var configuration = new GenerationConfiguration
        {
            ClientClassName = "clientName",
            OutputPath = tempPath,
            OpenAPIFilePath = Path.Combine(tempPath, "openapi.yaml"),
            ApiRootUrl = "https://graph.microsoft.com",
        };
        Directory.CreateDirectory(tempPath);
        await service.UpdateStateFromConfigurationAsync(
            configuration,
            "foo",
            new Dictionary<string, HashSet<string>> {
                { "/foo", new HashSet<string> { "GET" } }
            },
            Stream.Null);
        var result = await service.ShouldGenerateAsync(configuration, "foo");
        Assert.False(result);
    }
    [Fact]
    public async Task RemovesAClient()
    {
        var mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(mockLogger, httpClient, true, tempPath);
        var configuration = new GenerationConfiguration
        {
            ClientClassName = "clientName",
            OutputPath = tempPath,
            OpenAPIFilePath = Path.Combine(tempPath, "openapi.yaml"),
            ApiRootUrl = "https://graph.microsoft.com",
        };
        Directory.CreateDirectory(tempPath);
        await service.UpdateStateFromConfigurationAsync(
            configuration,
            "foo",
            new Dictionary<string, HashSet<string>> {
                { "/foo", new HashSet<string> { "GET" } }
            },
            Stream.Null);
        await service.RemoveClientAsync("clientName");
        var result = await service.IsConsumerPresent("clientName");
        Assert.False(result);
    }
    [Fact]
    public async Task RemovesAPlugin()
    {
        var mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(mockLogger, httpClient, true, tempPath);
        var configuration = new GenerationConfiguration
        {
            ClientClassName = "clientName",
            OutputPath = tempPath,
            OpenAPIFilePath = Path.Combine(tempPath, "openapi.yaml"),
            ApiRootUrl = "https://graph.microsoft.com",
            PluginTypes = [PluginType.APIManifest],
        };
        Directory.CreateDirectory(tempPath);
        await service.UpdateStateFromConfigurationAsync(
            configuration,
            "foo",
            new Dictionary<string, HashSet<string>> {
                { "/foo", new HashSet<string> { "GET" } }
            },
            Stream.Null);
        await service.RemovePluginAsync("clientName");
        var result = await service.IsConsumerPresent("clientName");
        Assert.False(result);
    }
    [Fact]
    public async Task FailsOnMigrateWithoutKiotaConfigMode()
    {
        var mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(mockLogger, httpClient, false, tempPath);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MigrateFromLockFileAsync(string.Empty, tempPath));
    }
    [Fact]
    public async Task FailsWhenTargetLockDirectoryIsNotSubDirectory()
    {
        var mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(mockLogger, httpClient, true, tempPath);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MigrateFromLockFileAsync(string.Empty, Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())));
    }
    [Fact]
    public async Task FailsWhenNoLockFilesAreFound()
    {
        var mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(mockLogger, httpClient, true, tempPath);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MigrateFromLockFileAsync(string.Empty, tempPath));
    }
    [Fact]
    public async Task FailsOnMultipleLockFilesAndClientName()
    {
        var mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(mockLogger, httpClient, true, tempPath);
        Directory.CreateDirectory(Path.Combine(tempPath, "client1"));
        Directory.CreateDirectory(Path.Combine(tempPath, "client2"));
        File.WriteAllText(Path.Combine(tempPath, "client1", LockManagementService.LockFileName), "foo");
        File.WriteAllText(Path.Combine(tempPath, "client2", LockManagementService.LockFileName), "foo");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MigrateFromLockFileAsync("bar", tempPath));
    }
    [Fact]
    public async Task MigratesAClient()
    {
        var mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(mockLogger, httpClient, true, tempPath);
        var descriptionPath = Path.Combine(tempPath, "description.yml");
        var generationConfiguration = new GenerationConfiguration
        {
            ClientClassName = "clientName",
            OutputPath = Path.Combine(tempPath, "client"),
            OpenAPIFilePath = descriptionPath,
            ApiRootUrl = "https://graph.microsoft.com",
        };
        Directory.CreateDirectory(generationConfiguration.OutputPath);
        await File.WriteAllTextAsync(descriptionPath, @$"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://localhost:443
paths:
  /enumeration:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                type: object
                properties:
                  bar:
                    type: object
                    properties:
                      foo:
                        type: string");
        var classicService = new WorkspaceManagementService(mockLogger, httpClient, false, tempPath);
        await classicService.UpdateStateFromConfigurationAsync(
            generationConfiguration,
            "foo",
            new Dictionary<string, HashSet<string>> {
                { "/foo", new HashSet<string> { "GET" } }
            },
            Stream.Null);
        var clientNames = await service.MigrateFromLockFileAsync("clientName", tempPath);
        Assert.Single(clientNames);
        Assert.Equal("clientName", clientNames.First());
        Assert.False(File.Exists(Path.Combine(tempPath, LockManagementService.LockFileName)));
        Assert.True(File.Exists(Path.Combine(tempPath, WorkspaceConfigurationStorageService.KiotaDirectorySegment, WorkspaceConfigurationStorageService.ConfigurationFileName)));
        Assert.True(File.Exists(Path.Combine(tempPath, WorkspaceConfigurationStorageService.KiotaDirectorySegment, WorkspaceConfigurationStorageService.ManifestFileName)));
        Assert.True(File.Exists(Path.Combine(tempPath, DescriptionStorageService.DescriptionsSubDirectoryRelativePath, "clientName", "description.yml")));
    }
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [Theory]
    public async Task GetsADescription(bool usesConfig, bool cleanOutput)
    {
        var mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(mockLogger, httpClient, usesConfig, tempPath);
        var descriptionPath = Path.Combine(tempPath, $"{DescriptionStorageService.DescriptionsSubDirectoryRelativePath}/clientName/description.yml");
        var outputPath = Path.Combine(tempPath, "client");
        Directory.CreateDirectory(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(descriptionPath));
        await File.WriteAllTextAsync(descriptionPath, @$"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://localhost:443
paths:
  /enumeration:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                type: object
                properties:
                  bar:
                    type: object
                    properties:
                      foo:
                        type: string");
        var descriptionCopy = await service.GetDescriptionCopyAsync("clientName", descriptionPath, cleanOutput);
        if (!usesConfig || cleanOutput)
            Assert.Null(descriptionCopy);
        else
            Assert.NotNull(descriptionCopy);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempPath))
            Directory.Delete(tempPath, true);
        httpClient.Dispose();
    }
}
