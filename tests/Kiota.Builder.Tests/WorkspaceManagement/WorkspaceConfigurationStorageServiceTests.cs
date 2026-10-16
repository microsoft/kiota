using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.WorkspaceManagement;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kiota.Builder.Tests.WorkspaceManagement;

[Collection(nameof(WorkspaceConfigurationStorageServiceTestsCollection))]
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
        workspace = Path.Join(tempPath, "workspace");
        outside = Path.Join(tempPath, "outside");
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
        Directory.CreateDirectory(Path.Join(outside, "client"));
        CreateDirectoryLink(Path.Join(workspace, "link"), outside, useJunction);
        Assert.Throws<InvalidOperationException>(() => BaseApiConsumerConfiguration.ValidateOutputPath("link", workspace));
        Assert.Throws<InvalidOperationException>(() => BaseApiConsumerConfiguration.ValidateOutputPath("link/client", workspace));
        Assert.Throws<InvalidOperationException>(() => BaseApiConsumerConfiguration.ValidateOutputPath("link/new/client", workspace));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ValidateOutputPathAcceptsDirectoryLinkInsideWorkspace(bool useJunction)
    {
        var inside = Path.Join(workspace, "real");
        Directory.CreateDirectory(inside);
        CreateDirectoryLink(Path.Join(workspace, "link"), inside, useJunction);
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
    public void ValidateOutputPathRejectsDirectoryLinkToWorkspaceRoot(bool useJunction)
    {
        Directory.CreateDirectory(workspace);
        CreateDirectoryLink(Path.Join(workspace, "link"), workspace, useJunction);
        Assert.Throws<InvalidOperationException>(() => BaseApiConsumerConfiguration.ValidateOutputPath("link", workspace));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ValidateOutputPathAcceptsDescendantThroughWorkspaceRootLink(bool useJunction)
    {
        Directory.CreateDirectory(Path.Join(workspace, "client"));
        CreateDirectoryLink(Path.Join(workspace, "link"), workspace, useJunction);
        BaseApiConsumerConfiguration.ValidateOutputPath("link/client", workspace);
        BaseApiConsumerConfiguration.ValidateOutputPath("link/new/client", workspace);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ValidateOutputPathRejectsLinkTargetWithLinkedParent(bool useJunction)
    {
        Directory.CreateDirectory(workspace);
        Directory.CreateDirectory(Path.Join(outside, "client"));
        CreateDirectoryLink(Path.Join(workspace, "escape"), outside, useJunction);
        CreateDirectoryLink(Path.Join(workspace, "output"), Path.Join(workspace, "escape", "client"), useJunction);
        Assert.Throws<InvalidOperationException>(() => BaseApiConsumerConfiguration.ValidateOutputPath("output", workspace));
        Assert.Throws<InvalidOperationException>(() => BaseApiConsumerConfiguration.ValidateOutputPath("output/new/client", workspace));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ValidateOutputPathAcceptsWorkspaceWithLinkedAncestor(bool useJunction)
    {
        var inside = Path.Join(workspace, "real");
        Directory.CreateDirectory(inside);
        CreateDirectoryLink(Path.Join(tempPath, "alias"), tempPath, useJunction);
        CreateDirectoryLink(Path.Join(workspace, "link"), inside, useJunction);
        var aliasedWorkspace = Path.Join(tempPath, "alias", "workspace");
        BaseApiConsumerConfiguration.ValidateOutputPath("link", aliasedWorkspace);
        BaseApiConsumerConfiguration.ValidateOutputPath("link/new/client", aliasedWorkspace);
        BaseApiConsumerConfiguration.ValidateOutputPath("new/client", aliasedWorkspace);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ValidateOutputPathRejectsDirectoryLinkCycle(bool useJunction)
    {
        Directory.CreateDirectory(workspace);
        var first = Path.Join(workspace, "first");
        var second = Path.Join(workspace, "second");
        Directory.CreateDirectory(second);
        CreateDirectoryLink(first, second, useJunction);
        Directory.Delete(second);
        CreateDirectoryLink(second, first, useJunction);
        Assert.Throws<InvalidOperationException>(() => BaseApiConsumerConfiguration.ValidateOutputPath("first", workspace));
    }
    [Theory]
    [InlineData("real", true)]
    [InlineData("../outside", false)]
    [InlineData(".", false)]
    public void ValidateOutputPathHandlesRelativeSymbolicLinkTargets(string target, bool isAllowed)
    {
        Directory.CreateDirectory(Path.Join(workspace, "real"));
        Directory.CreateDirectory(outside);
        CreateDirectoryLink(Path.Join(workspace, "link"), target, useJunction: false);
        if (isAllowed)
            BaseApiConsumerConfiguration.ValidateOutputPath("link", workspace);
        else
            Assert.Throws<InvalidOperationException>(() => BaseApiConsumerConfiguration.ValidateOutputPath("link", workspace));
    }
    [Fact]
    public void ValidateOutputPathRejectsRelativeTargetTraversingLinkedParentOnUnix()
    {
        if (OperatingSystem.IsWindows())
            Assert.Skip("This test exercises Unix symbolic link parent traversal.");
        Directory.CreateDirectory(workspace);
        Directory.CreateDirectory(Path.Join(outside, "child"));
        Directory.CreateDirectory(Path.Join(outside, "client"));
        CreateDirectoryLink(Path.Join(workspace, "redirect"), Path.Join(outside, "child"), useJunction: false);
        CreateDirectoryLink(Path.Join(workspace, "output"), "redirect/../client", useJunction: false);
        Assert.Throws<InvalidOperationException>(() => BaseApiConsumerConfiguration.ValidateOutputPath("output", workspace));
    }
    [Theory]
    [InlineData(false, "external")]
    [InlineData(true, "external")]
    [InlineData(false, "workspace")]
    [InlineData(true, "workspace")]
    [InlineData(false, "indirect")]
    [InlineData(true, "indirect")]
    public async Task GenerateClientRejectsSwappedOutputLinkBeforeDeletingFilesAsync(bool useJunction, string targetKind)
    {
        Directory.CreateDirectory(workspace);
        var originalDirectory = Directory.GetCurrentDirectory();
        var outputPath = Path.Join(workspace, "output");
        var targetPath = targetKind switch
        {
            "external" => outside,
            "workspace" => workspace,
            "indirect" => Path.Join(outside, "client"),
            _ => throw new ArgumentOutOfRangeException(nameof(targetKind)),
        };
        var nestedDirectory = Directory.CreateDirectory(Path.Join(targetPath, "data")).FullName;
        var looseFile = Path.Join(targetPath, "keep.txt");
        var nestedFile = Path.Join(nestedDirectory, "keep.txt");
        var cancellationToken = TestContext.Current.CancellationToken;
        await File.WriteAllTextAsync(looseFile, "loose file", cancellationToken);
        await File.WriteAllTextAsync(nestedFile, "nested file", cancellationToken);
        using var httpClient = new HttpClient();
        try
        {
            Directory.SetCurrentDirectory(workspace);
            var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, new GenerationConfiguration
            {
                OutputPath = outputPath,
                OpenAPIFilePath = Path.Join(tempPath, "missing.yaml"),
                CleanOutput = true,
                NoWorkspace = true,
            }, httpClient, useKiotaConfig: true);
            Directory.CreateDirectory(outputPath);
            BaseApiConsumerConfiguration.ValidateOutputPath("output", workspace);
            Directory.Delete(outputPath);
            if (targetKind == "indirect")
            {
                CreateDirectoryLink(Path.Join(workspace, "escape"), outside, useJunction);
                targetPath = Path.Join(workspace, "escape", "client");
            }
            CreateDirectoryLink(outputPath, targetPath, useJunction);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => builder.GenerateClientAsync(cancellationToken));

            var validationException = Assert.IsType<InvalidOperationException>(exception.InnerException);
            Assert.Contains("subdirectory of the workspace", validationException.Message, StringComparison.Ordinal);
            Assert.Equal("loose file", await File.ReadAllTextAsync(looseFile, cancellationToken));
            Assert.Equal("nested file", await File.ReadAllTextAsync(nestedFile, cancellationToken));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
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
        foreach (var link in directoryLinks.Where(static link => new DirectoryInfo(link).LinkTarget is not null))
            Directory.Delete(link);
        if (Directory.Exists(tempPath))
            Directory.Delete(tempPath, true);
        GC.SuppressFinalize(this);
    }

    [CollectionDefinition(nameof(WorkspaceConfigurationStorageServiceTestsCollection), DisableParallelization = true)]
    public sealed class WorkspaceConfigurationStorageServiceTestsCollection
    {
    }
}
