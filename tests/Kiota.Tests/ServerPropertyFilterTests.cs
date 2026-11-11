using System;
using System.IO;
using System.Threading.Tasks;
using kiota.Rpc;
using Kiota.Builder.Configuration;
using Kiota.Builder.Lock;
using Kiota.Builder.WorkspaceManagement;
using Xunit;

namespace Kiota.Tests;

public sealed class ServerPropertyFilterTests : IDisposable
{
    private readonly string workspacePath = Path.Combine(Directory.GetCurrentDirectory(), ".property-filter-rpc-tests-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task RestoresOnlyTheSavedWorkspaceClientRulesAndNeverReusesThemForAddsAsync()
    {
        var storage = new WorkspaceConfigurationStorageService(workspacePath);
        var workspace = new WorkspaceConfiguration();
        var first = new ApiClientConfiguration { OutputPath = "./first" };
        first.IncludeProperties.UnionWith(["User#name", "User#Name"]);
        first.ExcludeProperties.Add("User#mail");
        var second = new ApiClientConfiguration { OutputPath = "./second" };
        second.IncludeProperties.Add("Group#name");
        second.ExcludeProperties.Add("Group#members");
        workspace.Clients.Add("First", first);
        workspace.Clients.Add("Second", second);
        workspace.Clients.Add("Old", new() { OutputPath = "./old" });
        await storage.UpdateWorkspaceConfigurationAsync(workspace, null, TestContext.Current.CancellationToken);

        var configuration = new GenerationConfiguration { ClientClassName = "First", Operation = ConsumerOperation.Edit };
        configuration.IncludeProperties.Add("Global#unrelated");
        await Server.RestorePropertyFiltersAsync(configuration, true, workspacePath, TestContext.Current.CancellationToken);
        Assert.True(configuration.IncludeProperties.SetEquals(["User#name", "User#Name"]));
        Assert.True(configuration.ExcludeProperties.SetEquals(["User#mail"]));

        configuration.ClientClassName = "Second";
        configuration.Operation = ConsumerOperation.Generate;
        await Server.RestorePropertyFiltersAsync(configuration, true, workspacePath, TestContext.Current.CancellationToken);
        Assert.True(configuration.IncludeProperties.SetEquals(["Group#name"]));
        Assert.True(configuration.ExcludeProperties.SetEquals(["Group#members"]));

        foreach (var name in new[] { "Old", "Missing" })
        {
            configuration.ClientClassName = name;
            await Server.RestorePropertyFiltersAsync(configuration, true, workspacePath, TestContext.Current.CancellationToken);
            Assert.Empty(configuration.IncludeProperties);
            Assert.Empty(configuration.ExcludeProperties);
        }
        configuration.ClientClassName = "First";
        configuration.Operation = ConsumerOperation.Add;
        await Server.RestorePropertyFiltersAsync(configuration, true, workspacePath, TestContext.Current.CancellationToken);
        Assert.Empty(configuration.IncludeProperties);
        Assert.Empty(configuration.ExcludeProperties);
    }

    [Fact]
    public async Task RestoresLegacyRulesOnlyForTheMatchingClientAsync()
    {
        var configuration = new GenerationConfiguration
        {
            ClientClassName = "First",
            OutputPath = Path.Combine(workspacePath, "first"),
            OpenAPIFilePath = "https://example.com/openapi.yaml",
            Operation = ConsumerOperation.Generate,
        };
        configuration.IncludeProperties.UnionWith(["User#name", "User#Name"]);
        configuration.ExcludeProperties.Add("User#mail");
        Directory.CreateDirectory(configuration.OutputPath);
        await new LockManagementService().WriteLockFileAsync(configuration.OutputPath, new(configuration), TestContext.Current.CancellationToken);
        configuration.IncludeProperties.Clear();
        configuration.ExcludeProperties.Clear();
        await Server.RestorePropertyFiltersAsync(configuration, false, workspacePath, TestContext.Current.CancellationToken);
        Assert.True(configuration.IncludeProperties.SetEquals(["User#name", "User#Name"]));
        Assert.True(configuration.ExcludeProperties.SetEquals(["User#mail"]));
        configuration.ClientClassName = "Second";
        await Server.RestorePropertyFiltersAsync(configuration, false, workspacePath, TestContext.Current.CancellationToken);
        Assert.Empty(configuration.IncludeProperties);
        Assert.Empty(configuration.ExcludeProperties);
    }

    public void Dispose()
    {
        if (Directory.Exists(workspacePath))
            Directory.Delete(workspacePath, true);
        GC.SuppressFinalize(this);
    }
}
