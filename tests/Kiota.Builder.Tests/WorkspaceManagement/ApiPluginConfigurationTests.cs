using System;
using Kiota.Builder.Configuration;
using Kiota.Builder.WorkspaceManagement;
using Xunit;

namespace Kiota.Builder.Tests.WorkspaceManagement;

public sealed class ApiPluginConfigurationTests
{
    [Fact]
    public void Defensive()
    {
        Assert.Throws<ArgumentNullException>(() => new ApiPluginConfiguration(null));
    }
    [Fact]
    public void CopiesPluginTypesFromConfiguration()
    {
        var generationConfig = new GenerationConfiguration
        {
            PluginTypes = [PluginType.APIManifest]
        };
        var apiPluginConfig = new ApiPluginConfiguration(generationConfig);
        Assert.NotNull(apiPluginConfig);
        Assert.Contains("APIManifest", apiPluginConfig.Types);
    }
    [Fact]
    public void Clones()
    {
        var apiPluginConfig = new ApiPluginConfiguration
        {
            Types = ["APIManifest"]
        };
        var cloned = (ApiPluginConfiguration)apiPluginConfig.Clone();
        Assert.NotNull(cloned);
        Assert.Equal(apiPluginConfig.Types, cloned.Types);
    }
    [Fact]
    public void UpdateGenerationConfigurationFromPluginConfiguration()
    {
        var generationConfig = new GenerationConfiguration();
        var apiPluginConfig = new ApiPluginConfiguration
        {
            Types = ["APIManifest"]
        };
        apiPluginConfig.UpdateGenerationConfigurationFromApiPluginConfiguration(generationConfig, "Foo");
        Assert.Contains(PluginType.APIManifest, generationConfig.PluginTypes);
    }
}
