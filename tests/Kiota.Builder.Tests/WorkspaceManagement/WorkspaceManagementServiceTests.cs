using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Lock;
using Kiota.Builder.WorkspaceManagement;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests.WorkspaceManagement;

public sealed class WorkspaceManagementServiceTests : IDisposable
{
    [Fact]
    public async Task SkippedClientPropertyEditsKeepGenerationPendingAsync()
    {
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(Mock.Of<ILogger>(), httpClient, true, tempPath);
        var configuration = new GenerationConfiguration
        {
            ClientClassName = "Client",
            OpenAPIFilePath = Path.Combine(tempPath, "openapi.yaml"),
            OutputPath = Path.Combine(tempPath, "client"),
            ApiRootUrl = "https://example.com",
            IncludeProperties = ["User#ma*"],
            SkipGeneration = true,
        };
        var requests = new Dictionary<string, HashSet<string>> { { "/users", ["GET"] } };
        await service.UpdateStateFromConfigurationAsync(configuration, "description", requests, Stream.Null, TestContext.Current.CancellationToken);
        var generation = (GenerationConfiguration)configuration.Clone();
        generation.SkipGeneration = false;
        Assert.True(await service.ShouldGenerateAsync(generation, "description", TestContext.Current.CancellationToken));

        await service.UpdateStateFromConfigurationAsync(generation, "description", requests, Stream.Null, TestContext.Current.CancellationToken);
        Assert.False(await service.ShouldGenerateAsync(generation, "description", TestContext.Current.CancellationToken));

        configuration.IncludeProperties = ["User#displayName"];
        await service.UpdateStateFromConfigurationAsync(configuration, "description", requests, Stream.Null, TestContext.Current.CancellationToken);
        generation = (GenerationConfiguration)configuration.Clone();
        generation.SkipGeneration = false;
        Assert.True(await service.ShouldGenerateAsync(generation, "description", TestContext.Current.CancellationToken));
        await service.UpdateStateFromConfigurationAsync(generation, "description", requests, Stream.Null, TestContext.Current.CancellationToken);
        Assert.False(await service.ShouldGenerateAsync(generation, "description", TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PropertySelectionsInvalidateGenerationOnlyWhenTheirOrdinalContentsChangeAsync(bool usesConfig)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), ".property-filter-state-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var service = new WorkspaceManagementService(Mock.Of<ILogger>(), httpClient, usesConfig, path);
            var configuration = new GenerationConfiguration
            {
                ClientClassName = "Client",
                OutputPath = Path.Combine(path, "client"),
                OpenAPIFilePath = Path.Combine(path, "openapi.yaml"),
                ApiRootUrl = "https://example.com",
            };
            configuration.IncludeProperties.UnionWith(["User#name", "User#Name"]);
            configuration.ExcludeProperties.UnionWith(["User#mail", "User#id"]);
            var requests = new Dictionary<string, HashSet<string>>
            {
                { "/users", ["GET"] },
            };
            Directory.CreateDirectory(configuration.OutputPath);
            await service.UpdateStateFromConfigurationAsync(configuration, "unchanged-description", requests, Stream.Null, TestContext.Current.CancellationToken);

            var reordered = (GenerationConfiguration)configuration.Clone();
            reordered.IncludeProperties = new(["User#Name", "User#name", "User#Name"], StringComparer.Ordinal);
            reordered.ExcludeProperties = new(["User#id", "User#mail"], StringComparer.Ordinal);
            Assert.False(await service.ShouldGenerateAsync(reordered, "unchanged-description", TestContext.Current.CancellationToken));

            var caseChanged = (GenerationConfiguration)configuration.Clone();
            caseChanged.IncludeProperties.Remove("User#Name");
            caseChanged.IncludeProperties.Add("user#Name");
            Assert.True(await service.ShouldGenerateAsync(caseChanged, "unchanged-description", TestContext.Current.CancellationToken));
            caseChanged = (GenerationConfiguration)configuration.Clone();
            caseChanged.ExcludeProperties.Remove("User#id");
            caseChanged.ExcludeProperties.Add("User#Id");
            Assert.True(await service.ShouldGenerateAsync(caseChanged, "unchanged-description", TestContext.Current.CancellationToken));

            var cleared = (GenerationConfiguration)configuration.Clone();
            cleared.IncludeProperties.Clear();
            cleared.ExcludeProperties.Clear();
            Assert.True(await service.ShouldGenerateAsync(cleared, "unchanged-description", TestContext.Current.CancellationToken));
            await service.UpdateStateFromConfigurationAsync(cleared, "unchanged-description", requests, Stream.Null, TestContext.Current.CancellationToken);
            Assert.False(await service.ShouldGenerateAsync(cleared, "unchanged-description", TestContext.Current.CancellationToken));
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }

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
    public async Task IsClientPresentReturnsFalseOnNoClientAsync(bool usesConfig)
    {
        var mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(mockLogger, httpClient, usesConfig, tempPath);
        var result = await service.IsConsumerPresentAsync("clientName", cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(result);
    }
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [Theory]
    public async Task ShouldGenerateReturnsTrueAsync(bool usesConfig, bool cleanOutput)
    {
        var mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(mockLogger, httpClient, usesConfig, tempPath);
        var configuration = new GenerationConfiguration
        {
            ClientClassName = "clientName",
            OutputPath = Path.Combine(tempPath, "client"),
            OpenAPIFilePath = Path.Combine(tempPath, "openapi.yaml"),
            CleanOutput = cleanOutput,
        };
        var result = await service.ShouldGenerateAsync(configuration, "foo", cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(result);
    }
    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public async Task ShouldGenerateReturnsFalseAsync(bool usesConfig)
    {
        var mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(mockLogger, httpClient, usesConfig, tempPath);
        var configuration = new GenerationConfiguration
        {
            ClientClassName = "clientName",
            OutputPath = Path.Combine(tempPath, "client"),
            OpenAPIFilePath = Path.Combine(tempPath, "openapi.yaml"),
            ApiRootUrl = "https://graph.microsoft.com",
        };
        Directory.CreateDirectory(tempPath);
        Directory.CreateDirectory(configuration.OutputPath);
        await service.UpdateStateFromConfigurationAsync(
            configuration,
            "foo",
            new Dictionary<string, HashSet<string>> {
                { "/foo", new HashSet<string> { "GET" } }
            },
            Stream.Null, cancellationToken: TestContext.Current.CancellationToken);
        var result = await service.ShouldGenerateAsync(configuration, "foo", cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(result);
    }
    [Fact]
    public async Task RemovesAClientAsync()
    {
        var mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(mockLogger, httpClient, true, tempPath);
        var configuration = new GenerationConfiguration
        {
            ClientClassName = "clientName",
            OutputPath = Path.Combine(tempPath, "client"),
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
            Stream.Null, cancellationToken: TestContext.Current.CancellationToken);
        await service.RemoveClientAsync("clientName", cancellationToken: TestContext.Current.CancellationToken);
        var result = await service.IsConsumerPresentAsync("clientName", cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(result);
    }
    [Fact]
    public async Task RemovesAPluginAsync()
    {
        var mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(mockLogger, httpClient, true, tempPath);
        var configuration = new GenerationConfiguration
        {
            ClientClassName = "clientName",
            OutputPath = Path.Combine(tempPath, "plugin"),
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
            Stream.Null, cancellationToken: TestContext.Current.CancellationToken);
        await service.RemovePluginAsync("clientName", cancellationToken: TestContext.Current.CancellationToken);
        var result = await service.IsConsumerPresentAsync("clientName", cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(result);
    }
    [Fact]
    public async Task FailsOnMigrateWithoutKiotaConfigModeAsync()
    {
        var mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(mockLogger, httpClient, false, tempPath);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MigrateFromLockFileAsync(string.Empty, tempPath, cancellationToken: TestContext.Current.CancellationToken));
    }
    [Fact]
    public async Task FailsWhenTargetLockDirectoryIsNotSubDirectoryAsync()
    {
        var mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(mockLogger, httpClient, true, tempPath);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MigrateFromLockFileAsync(string.Empty, Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), cancellationToken: TestContext.Current.CancellationToken));
    }
    [Fact]
    public async Task FailsWhenNoLockFilesAreFoundAsync()
    {
        var mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(mockLogger, httpClient, true, tempPath);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MigrateFromLockFileAsync(string.Empty, tempPath, cancellationToken: TestContext.Current.CancellationToken));
    }
    [Fact]
    public async Task FailsOnMultipleLockFilesAndClientNameAsync()
    {
        var mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(mockLogger, httpClient, true, tempPath);
        Directory.CreateDirectory(Path.Combine(tempPath, "client1"));
        Directory.CreateDirectory(Path.Combine(tempPath, "client2"));
        File.WriteAllText(Path.Combine(tempPath, "client1", LockManagementService.LockFileName), "foo");
        File.WriteAllText(Path.Combine(tempPath, "client2", LockManagementService.LockFileName), "foo");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MigrateFromLockFileAsync("bar", tempPath, cancellationToken: TestContext.Current.CancellationToken));
    }
    [Fact]
    public async Task MigratesAClientAsync()
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
        generationConfiguration.IncludeProperties.UnionWith(["User#name", "User#Name"]);
        generationConfiguration.ExcludeProperties.UnionWith(["User#mail", "user#mail"]);
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
                        type: string", cancellationToken: TestContext.Current.CancellationToken);
        var classicService = new WorkspaceManagementService(mockLogger, httpClient, false, tempPath);
        await classicService.UpdateStateFromConfigurationAsync(
            generationConfiguration,
            "foo",
            new Dictionary<string, HashSet<string>> {
                { "/foo", new HashSet<string> { "GET" } }
            },
            Stream.Null, cancellationToken: TestContext.Current.CancellationToken);
        var clientNames = await service.MigrateFromLockFileAsync("clientName", tempPath, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(clientNames);
        Assert.Equal("clientName", clientNames.First());
        Assert.False(File.Exists(Path.Combine(tempPath, LockManagementService.LockFileName)));
        Assert.True(File.Exists(Path.Combine(tempPath, WorkspaceConfigurationStorageService.KiotaDirectorySegment, WorkspaceConfigurationStorageService.ConfigurationFileName)));
        Assert.True(File.Exists(Path.Combine(tempPath, WorkspaceConfigurationStorageService.KiotaDirectorySegment, WorkspaceConfigurationStorageService.ManifestFileName)));
        Assert.True(File.Exists(Path.Combine(tempPath, DescriptionStorageService.DescriptionsSubDirectoryRelativePath, "clientName", "openapi.yml")));
        var (workspace, _) = await new WorkspaceConfigurationStorageService(tempPath).GetWorkspaceConfigurationAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(workspace);
        var migrated = new GenerationConfiguration();
        workspace.Clients["clientName"].UpdateGenerationConfigurationFromApiClientConfiguration(migrated, "clientName");
        Assert.True(migrated.IncludeProperties.SetEquals(generationConfiguration.IncludeProperties));
        Assert.True(migrated.ExcludeProperties.SetEquals(generationConfiguration.ExcludeProperties));
    }
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [Theory]
    public async Task GetsADescriptionAsync(bool usesConfig, bool cleanOutput)
    {
        var mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(mockLogger, httpClient, usesConfig, tempPath);
        var descriptionPath = Path.Combine(tempPath, $"{DescriptionStorageService.DescriptionsSubDirectoryRelativePath}/clientName/openapi.yml");
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
                        type: string", cancellationToken: TestContext.Current.CancellationToken);
        var descriptionCopy = await service.GetDescriptionCopyAsync("clientName", descriptionPath, cleanOutput, cancellationToken: TestContext.Current.CancellationToken);
        if (!usesConfig || cleanOutput)
            Assert.Null(descriptionCopy);
        else
            Assert.NotNull(descriptionCopy);
    }

    [Theory]
    [InlineData("junk/../Victim")]
    [InlineData("../Victim")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    public async Task RemoveClientRejectsTraversalNamesAsync(string clientName)
    {
        var mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(mockLogger, httpClient, true, tempPath);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RemoveClientAsync(clientName, cancellationToken: TestContext.Current.CancellationToken));
    }
    [Theory]
    [InlineData("junk/../Victim")]
    [InlineData("../Victim")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    public async Task RemovePluginRejectsTraversalNamesAsync(string clientName)
    {
        var mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(mockLogger, httpClient, true, tempPath);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RemovePluginAsync(clientName, cancellationToken: TestContext.Current.CancellationToken));
    }
    [Theory]
    [InlineData("junk/../Victim")]
    [InlineData("../Victim")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    public async Task IsConsumerPresentRejectsTraversalNamesAsync(string clientName)
    {
        var mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(mockLogger, httpClient, true, tempPath);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.IsConsumerPresentAsync(clientName, cancellationToken: TestContext.Current.CancellationToken));
    }
    [Theory]
    [InlineData("junk/../Victim")]
    [InlineData("../Victim")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    public async Task GetDescriptionCopyRejectsTraversalNamesAsync(string clientName)
    {
        var mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(mockLogger, httpClient, true, tempPath);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetDescriptionCopyAsync(clientName, Path.Combine(tempPath, "openapi.yml"), false, cancellationToken: TestContext.Current.CancellationToken));
    }
    [Theory]
    [InlineData("junk/../Victim")]
    [InlineData("../Victim")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    public async Task UpdateStateRejectsTraversalClientNamesAsync(string clientName)
    {
        var mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        var service = new WorkspaceManagementService(mockLogger, httpClient, true, tempPath);
        var configuration = new GenerationConfiguration
        {
            ClientClassName = clientName,
            OutputPath = Path.Combine(tempPath, "client"),
            OpenAPIFilePath = Path.Combine(tempPath, "openapi.yaml"),
            ApiRootUrl = "https://graph.microsoft.com",
        };
        using var stream = new MemoryStream();
        stream.WriteByte(0x1);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateStateFromConfigurationAsync(
            configuration,
            "foo",
            new Dictionary<string, HashSet<string>> {
                { "/foo", new HashSet<string> { "GET" } }
            },
            stream, cancellationToken: TestContext.Current.CancellationToken));
    }

    public void Dispose()
    {
        if (Directory.Exists(tempPath))
            Directory.Delete(tempPath, true);
        httpClient.Dispose();
    }
}
