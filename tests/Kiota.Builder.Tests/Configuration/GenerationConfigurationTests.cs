using System.Collections.Generic;
using System.IO;
using Kiota.Builder.Configuration;
using Xunit;

namespace Kiota.Builder.Tests.Configuration;

public class GenerationConfigurationTests
{
    [Fact]
    public void Clones()
    {
        var generationConfiguration = new GenerationConfiguration
        {
            ClientClassName = "class1",
            IncludePatterns = null,
        };
        var clone = generationConfiguration.Clone() as GenerationConfiguration;
        Assert.NotNull(clone);
        Assert.Equal(generationConfiguration.ClientClassName, clone.ClientClassName);
        Assert.NotNull(clone.IncludePatterns);
        Assert.Empty(clone.IncludePatterns);
        clone.ClientClassName = "class2";
        Assert.NotEqual(generationConfiguration.ClientClassName, clone.ClientClassName);
    }
    [Fact]
    public void ToApiDependency()
    {
        var generationConfiguration = new GenerationConfiguration
        {
            ClientClassName = "class1",
            IncludePatterns = null,
            OpenAPIFilePath = "https://pet.store/openapi.yaml",
            ApiRootUrl = "https://pet.store/api",
        };
        var apiDependency = generationConfiguration.ToApiDependency("foo", new Dictionary<string, HashSet<string>>{
            { "foo/bar", new HashSet<string>{"GET"}}
        }, Path.GetTempPath());
        Assert.NotNull(apiDependency);
        Assert.NotNull(apiDependency.Extensions);
        Assert.Equal("foo", apiDependency.Extensions[GenerationConfiguration.KiotaHashManifestExtensionKey].GetValue<string>());
        Assert.NotEmpty(apiDependency.Requests);
        Assert.Equal("foo/bar", apiDependency.Requests[0].UriTemplate);
        Assert.Equal("GET", apiDependency.Requests[0].Method);
    }
    [Fact]
    public void ToApiDependencyDoesNotIncludeConfigHashIfEmpty()
    {
        var generationConfiguration = new GenerationConfiguration
        {
            ClientClassName = "class1",
            IncludePatterns = null,
            OpenAPIFilePath = "https://pet.store/openapi.yaml",
            ApiRootUrl = "https://pet.store/api",
        };
        var apiDependency = generationConfiguration.ToApiDependency(string.Empty, new Dictionary<string, HashSet<string>>{
            { "foo/bar", new HashSet<string>{"GET"}}
        }, Path.GetTempPath());
        Assert.NotNull(apiDependency);
        Assert.NotNull(apiDependency.Extensions);
        Assert.False(apiDependency.Extensions.ContainsKey(GenerationConfiguration.KiotaHashManifestExtensionKey));
        Assert.NotEmpty(apiDependency.Requests);
        Assert.Equal("foo/bar", apiDependency.Requests[0].UriTemplate);
        Assert.Equal("GET", apiDependency.Requests[0].Method);
    }
}
