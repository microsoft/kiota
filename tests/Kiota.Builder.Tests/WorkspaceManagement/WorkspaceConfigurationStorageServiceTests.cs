using System;
using System.IO;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.WorkspaceManagement;
using Xunit;

namespace Kiota.Builder.Tests.WorkspaceManagement;

public sealed class WorkspaceConfigurationStorageServiceTests : IDisposable
{
    [Theory]
    [InlineData("")]
    [InlineData(""","includeProperties":[],"excludeProperties":[]""")]
    [InlineData(""","includeProperties":["User#name","User#Name","User#name"],"excludeProperties":["User#mail","user#mail"]""")]
    public async Task RoundTripsPropertySelectionsAndClearsAbsentClientSettingsAsync(string propertySettings)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), ".property-filter-workspace-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var service = new WorkspaceConfigurationStorageService(path);
            Directory.CreateDirectory(service.TargetDirectory);
            var configurationPath = Path.Combine(service.TargetDirectory, WorkspaceConfigurationStorageService.ConfigurationFileName);
            await File.WriteAllTextAsync(configurationPath, $$"""
                {
                  "clients": {
                    "Filtered": { "outputPath": "./filtered"{{propertySettings}} },
                    "Unfiltered": { "outputPath": "./unfiltered" }
                  }
                }
                """, TestContext.Current.CancellationToken);
            var (workspace, _) = await service.GetWorkspaceConfigurationAsync(TestContext.Current.CancellationToken);
            Assert.NotNull(workspace);
            var generation = new GenerationConfiguration();
            workspace.Clients["Filtered"].UpdateGenerationConfigurationFromApiClientConfiguration(generation, "Filtered");
            var expectedCount = propertySettings.Contains("User#name", StringComparison.Ordinal) ? 2 : 0;
            Assert.Equal(expectedCount, generation.IncludeProperties.Count);
            Assert.Equal(expectedCount, generation.ExcludeProperties.Count);
            await service.UpdateWorkspaceConfigurationAsync(workspace, null, TestContext.Current.CancellationToken);
            var (roundTrip, _) = await service.GetWorkspaceConfigurationAsync(TestContext.Current.CancellationToken);
            Assert.NotNull(roundTrip);
            Assert.Equal(workspace.Clients["Filtered"], roundTrip.Clients["Filtered"], new ApiClientConfigurationComparer());
            workspace.Clients["Unfiltered"].UpdateGenerationConfigurationFromApiClientConfiguration(generation, "Unfiltered");
            Assert.Empty(generation.IncludeProperties);
            Assert.Empty(generation.ExcludeProperties);
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }

    [Fact]
    public async Task DefensiveProgrammingAsync()
    {
        Assert.Throws<ArgumentException>(() => new WorkspaceConfigurationStorageService(string.Empty));
        var service = new WorkspaceConfigurationStorageService(Directory.GetCurrentDirectory());
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.UpdateWorkspaceConfigurationAsync(null, null, cancellationToken: TestContext.Current.CancellationToken));
    }
    private readonly string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    [Fact]
    public async Task InitializesAsync()
    {
        var service = new WorkspaceConfigurationStorageService(tempPath);
        await service.InitializeAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(File.Exists(Path.Combine(tempPath, WorkspaceConfigurationStorageService.KiotaDirectorySegment, WorkspaceConfigurationStorageService.ConfigurationFileName)));
    }
    [Fact]
    public async Task FailsOnDoubleInitAsync()
    {
        var service = new WorkspaceConfigurationStorageService(tempPath);
        await service.InitializeAsync(cancellationToken: TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.InitializeAsync(cancellationToken: TestContext.Current.CancellationToken));
    }
    [Fact]
    public async Task ReturnsNullOnNonInitializedAsync()
    {
        var service = new WorkspaceConfigurationStorageService(tempPath);
        var (config, manifest) = await service.GetWorkspaceConfigurationAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Null(config);
        Assert.Null(manifest);
    }
    [Fact]
    public async Task ReturnsConfigurationWhenInitializedAsync()
    {
        var service = new WorkspaceConfigurationStorageService(tempPath);
        await service.InitializeAsync(cancellationToken: TestContext.Current.CancellationToken);
        var (result, manifest) = await service.GetWorkspaceConfigurationAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Null(manifest);
    }
    [Fact]
    public async Task ReturnsIsInitializedAsync()
    {
        var service = new WorkspaceConfigurationStorageService(tempPath);
        await service.InitializeAsync(cancellationToken: TestContext.Current.CancellationToken);
        var result = await service.IsInitializedAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(result);
    }
    [Fact]
    public async Task DoesNotReturnIsInitializedAsync()
    {
        var service = new WorkspaceConfigurationStorageService(tempPath);
        var result = await service.IsInitializedAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(result);
    }
    [Fact]
    public async Task BackupsAndRestoresAsync()
    {
        var service = new WorkspaceConfigurationStorageService(tempPath);
        await service.InitializeAsync(cancellationToken: TestContext.Current.CancellationToken);
        await service.BackupConfigAsync(cancellationToken: TestContext.Current.CancellationToken);
        var targetConfigFile = Path.Combine(tempPath, WorkspaceConfigurationStorageService.KiotaDirectorySegment, WorkspaceConfigurationStorageService.ConfigurationFileName);
        File.Delete(targetConfigFile);
        Assert.False(File.Exists(targetConfigFile));
        await service.RestoreConfigAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(File.Exists(targetConfigFile));
    }
    [InlineData("../outside")]
    [InlineData("client/../outside")]
    [InlineData(".")]
    [Theory]
    public async Task RejectsClientOutputPathOutsideWorkspaceAsync(string outputPath)
    {
        var service = new WorkspaceConfigurationStorageService(tempPath);
        await WriteWorkspaceConfigurationAsync($$"""
        {
          "version": "1.0.0",
          "clients": {
            "GraphClient": {
              "outputPath": "{{outputPath}}"
            }
          },
          "plugins": {}
        }
        """);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetWorkspaceConfigurationAsync(cancellationToken: TestContext.Current.CancellationToken));
    }
    [Fact]
    public async Task RejectsPluginRootedOutputPathAsync()
    {
        var service = new WorkspaceConfigurationStorageService(tempPath);
        await WriteWorkspaceConfigurationAsync($$"""
        {
          "version": "1.0.0",
          "clients": {},
          "plugins": {
            "GraphPlugin": {
              "outputPath": "{{Path.GetFullPath(Path.Combine(tempPath, "plugin")).Replace('\\', '/')}}"
            }
          }
        }
        """);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetWorkspaceConfigurationAsync(cancellationToken: TestContext.Current.CancellationToken));
    }
    [InlineData("../Victim")]
    [InlineData("junk/../Victim")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [Theory]
    public async Task RejectsClientNameWithTraversalKeyAsync(string clientName)
    {
        var service = new WorkspaceConfigurationStorageService(tempPath);
        await WriteWorkspaceConfigurationAsync($$"""
        {
          "version": "1.0.0",
          "clients": {
            "{{clientName.Replace("\\", "\\\\", StringComparison.Ordinal)}}": {
              "outputPath": "./client"
            }
          },
          "plugins": {}
        }
        """);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetWorkspaceConfigurationAsync(cancellationToken: TestContext.Current.CancellationToken));
    }
    [InlineData("../Victim")]
    [InlineData("junk/../Victim")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [Theory]
    public async Task RejectsPluginNameWithTraversalKeyAsync(string pluginName)
    {
        var service = new WorkspaceConfigurationStorageService(tempPath);
        await WriteWorkspaceConfigurationAsync($$"""
        {
          "version": "1.0.0",
          "clients": {},
          "plugins": {
            "{{pluginName.Replace("\\", "\\\\", StringComparison.Ordinal)}}": {
              "outputPath": "./plugin"
            }
          }
        }
        """);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetWorkspaceConfigurationAsync(cancellationToken: TestContext.Current.CancellationToken));
    }
    private async Task WriteWorkspaceConfigurationAsync(string content)
    {
        var configurationDirectory = Path.Combine(tempPath, WorkspaceConfigurationStorageService.KiotaDirectorySegment);
        Directory.CreateDirectory(configurationDirectory);
        await File.WriteAllTextAsync(Path.Combine(configurationDirectory, WorkspaceConfigurationStorageService.ConfigurationFileName), content, TestContext.Current.CancellationToken);
    }
    public void Dispose()
    {
        if (Directory.Exists(tempPath))
            Directory.Delete(tempPath, true);
        GC.SuppressFinalize(this);
    }
}
