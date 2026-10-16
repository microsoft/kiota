using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.WorkspaceManagement;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kiota.Builder.Tests.WorkspaceManagement;

public sealed class WorkspaceConfigurationStorageServiceTests : IDisposable
{
    [Fact]
    public async Task DefensiveProgrammingAsync()
    {
        Assert.Throws<ArgumentException>(() => new WorkspaceConfigurationStorageService(string.Empty));
        var service = new WorkspaceConfigurationStorageService(Directory.GetCurrentDirectory());
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.UpdateWorkspaceConfigurationAsync(null, null, cancellationToken: TestContext.Current.CancellationToken));
    }
    private readonly string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly string workspace;
    private readonly string outside;
    private readonly List<string> directoryLinks = [];
    public WorkspaceConfigurationStorageServiceTests()
    {
        workspace = Path.Combine(tempPath, "workspace");
        outside = Path.Combine(tempPath, "outside");
    }
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
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ValidateOutputPathRejectsDirectoryLinkEscapingWorkspace(bool useJunction)
    {
        Directory.CreateDirectory(workspace);
        Directory.CreateDirectory(Path.Combine(outside, "client"));
        CreateDirectoryLink(Path.Combine(workspace, "link"), outside, useJunction);
        Assert.Throws<InvalidOperationException>(() => BaseApiConsumerConfiguration.ValidateOutputPath("link", workspace));
        Assert.Throws<InvalidOperationException>(() => BaseApiConsumerConfiguration.ValidateOutputPath("link/client", workspace));
        Assert.Throws<InvalidOperationException>(() => BaseApiConsumerConfiguration.ValidateOutputPath("link/new/client", workspace));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ValidateOutputPathAcceptsDirectoryLinkInsideWorkspace(bool useJunction)
    {
        var inside = Path.Combine(workspace, "real");
        Directory.CreateDirectory(inside);
        CreateDirectoryLink(Path.Combine(workspace, "link"), inside, useJunction);
        BaseApiConsumerConfiguration.ValidateOutputPath("link", workspace);
        BaseApiConsumerConfiguration.ValidateOutputPath("link/new/client", workspace);
    }
    [Fact]
    public void ValidateOutputPathAcceptsPlainSubdirectory()
    {
        Directory.CreateDirectory(workspace);
        BaseApiConsumerConfiguration.ValidateOutputPath("src/client", workspace);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GenerateClientRejectsSwappedOutputLinkBeforeDeletingExternalFilesAsync(bool useJunction)
    {
        var workspaceDirectory = Directory.GetCurrentDirectory();
        var outputPath = Path.Combine(workspaceDirectory, Path.GetRandomFileName());
        var nestedDirectory = Directory.CreateDirectory(Path.Combine(outside, "data")).FullName;
        var looseFile = Path.Combine(outside, "keep.txt");
        var nestedFile = Path.Combine(nestedDirectory, "keep.txt");
        var cancellationToken = TestContext.Current.CancellationToken;
        await File.WriteAllTextAsync(looseFile, "loose file", cancellationToken);
        await File.WriteAllTextAsync(nestedFile, "nested file", cancellationToken);
        using var httpClient = new HttpClient();
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, new GenerationConfiguration
        {
            OutputPath = outputPath,
            OpenAPIFilePath = Path.Combine(tempPath, "missing.yaml"),
            CleanOutput = true,
            NoWorkspace = true,
        }, httpClient, useKiotaConfig: true);
        try
        {
            Directory.CreateDirectory(outputPath);
            BaseApiConsumerConfiguration.ValidateOutputPath(Path.GetRelativePath(workspaceDirectory, outputPath), workspaceDirectory);
            Directory.Delete(outputPath);
            CreateDirectoryLink(outputPath, outside, useJunction);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => builder.GenerateClientAsync(cancellationToken));

            var validationException = Assert.IsType<InvalidOperationException>(exception.InnerException);
            Assert.Contains("cannot traverse symbolic links or junctions outside of it", validationException.Message, StringComparison.Ordinal);
            Assert.Equal("loose file", await File.ReadAllTextAsync(looseFile, cancellationToken));
            Assert.Equal("nested file", await File.ReadAllTextAsync(nestedFile, cancellationToken));
        }
        finally
        {
            if (Directory.Exists(outputPath) || new DirectoryInfo(outputPath).LinkTarget is not null)
                Directory.Delete(outputPath);
        }
    }
    private void CreateDirectoryLink(string linkPath, string targetPath, bool useJunction)
    {
        if (useJunction)
        {
            if (!OperatingSystem.IsWindows())
                Assert.Skip("Directory junctions are only supported on Windows.");
            var startInfo = new ProcessStartInfo("cmd.exe")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            startInfo.ArgumentList.Add("/c");
            startInfo.ArgumentList.Add("mklink");
            startInfo.ArgumentList.Add("/J");
            startInfo.ArgumentList.Add(linkPath);
            startInfo.ArgumentList.Add(targetPath);
            using var process = Process.Start(startInfo);
            Assert.NotNull(process);
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"Junction creation failed: {output}{error}");
            directoryLinks.Add(linkPath);
            Assert.NotNull(new DirectoryInfo(linkPath).LinkTarget);
            return;
        }
        try
        {
            Directory.CreateSymbolicLink(linkPath, targetPath);
            directoryLinks.Add(linkPath);
        }
        catch (UnauthorizedAccessException ex) when (OperatingSystem.IsWindows())
        {
            Assert.Skip($"Symbolic link creation is not permitted: {ex.Message}");
        }
        catch (IOException ex) when (OperatingSystem.IsWindows() && (ex.HResult & 0xFFFF) == 1314)
        {
            Assert.Skip($"Symbolic link creation requires a privilege not held by this process: {ex.Message}");
        }
    }
    private async Task WriteWorkspaceConfigurationAsync(string content)
    {
        var configurationDirectory = Path.Combine(tempPath, WorkspaceConfigurationStorageService.KiotaDirectorySegment);
        Directory.CreateDirectory(configurationDirectory);
        await File.WriteAllTextAsync(Path.Combine(configurationDirectory, WorkspaceConfigurationStorageService.ConfigurationFileName), content, TestContext.Current.CancellationToken);
    }
    public void Dispose()
    {
        foreach (var link in directoryLinks)
            if (new DirectoryInfo(link).LinkTarget is not null)
                Directory.Delete(link);
        if (Directory.Exists(tempPath))
            Directory.Delete(tempPath, true);
        GC.SuppressFinalize(this);
    }
}
