using System;
using Kiota.Builder.Configuration;
using Kiota.Builder.WorkspaceManagement;
using Xunit;

namespace Kiota.Builder.Tests.WorkspaceManagement;

public sealed class ApiClientConfigurationTests
{
    [Fact]
    public void Clones()
    {
        var clientConfig = new ApiClientConfiguration
        {
            ClientNamespaceName = "foo",
            DescriptionLocation = "bar",
            ExcludeBackwardCompatible = true,
            ExcludePatterns = [
                "exclude"
            ],
            IncludeAdditionalData = true,
            IncludePatterns = [
                "include"
            ],
            Language = "csharp",
            OutputPath = "output",
            StructuredMimeTypes = [
                "mime"
            ],
            UsesBackingStore = true,
        };
        var cloned = (ApiClientConfiguration)clientConfig.Clone();
        Assert.NotNull(cloned);
        Assert.Equal(clientConfig.ClientNamespaceName, cloned.ClientNamespaceName);
        Assert.Equal(clientConfig.DescriptionLocation, cloned.DescriptionLocation);
        Assert.Equal(clientConfig.ExcludeBackwardCompatible, cloned.ExcludeBackwardCompatible);
        Assert.Equal(clientConfig.ExcludePatterns, cloned.ExcludePatterns);
        Assert.Equal(clientConfig.IncludeAdditionalData, cloned.IncludeAdditionalData);
        Assert.Equal(clientConfig.IncludePatterns, cloned.IncludePatterns);
        Assert.Equal(clientConfig.Language, cloned.Language);
        Assert.Equal(clientConfig.OutputPath, cloned.OutputPath);
        Assert.Equal(clientConfig.StructuredMimeTypes, cloned.StructuredMimeTypes);
        Assert.Equal(clientConfig.UsesBackingStore, cloned.UsesBackingStore);
    }

    [Fact]
    public void CreatesApiClientConfigurationFromGenerationConfiguration()
    {
        var generationConfiguration = new GenerationConfiguration
        {
            ApiManifestPath = "manifest",
            ClientClassName = "client",
            ClientNamespaceName = "namespace",
            ExcludeBackwardCompatible = true,
            ExcludePatterns = ["exclude"],
            IncludeAdditionalData = true,
            IncludePatterns = ["include"],
            Language = GenerationLanguage.CSharp,
            OpenAPIFilePath = "openapi",
            OutputPath = "output",
            UsesBackingStore = true,
            StructuredMimeTypes = ["application/json"],
        };
        var clientConfig = new ApiClientConfiguration(generationConfiguration);
        Assert.NotNull(clientConfig);
        Assert.Equal(generationConfiguration.ClientNamespaceName, clientConfig.ClientNamespaceName);
        Assert.Equal(generationConfiguration.OpenAPIFilePath, clientConfig.DescriptionLocation);
        Assert.Equal(generationConfiguration.ExcludeBackwardCompatible, clientConfig.ExcludeBackwardCompatible);
        Assert.Equal(generationConfiguration.ExcludePatterns, clientConfig.ExcludePatterns);
        Assert.Equal(generationConfiguration.IncludeAdditionalData, clientConfig.IncludeAdditionalData);
        Assert.Equal(generationConfiguration.IncludePatterns, clientConfig.IncludePatterns);
        Assert.Equal(generationConfiguration.Language.ToString(), clientConfig.Language, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(generationConfiguration.OutputPath, clientConfig.OutputPath);
        Assert.Equal(generationConfiguration.StructuredMimeTypes, clientConfig.StructuredMimeTypes);
        Assert.Equal(generationConfiguration.UsesBackingStore, clientConfig.UsesBackingStore);
    }
    [Fact]
    public void UpdatesGenerationConfigurationFromApiClientConfiguration()
    {
        var clientConfiguration = new ApiClientConfiguration
        {
            ClientNamespaceName = "namespace",
            DescriptionLocation = "openapi",
            ExcludeBackwardCompatible = true,
            ExcludePatterns = ["exclude"],
            IncludeAdditionalData = true,
            IncludePatterns = ["include"],
            Language = "csharp",
            OutputPath = "output",
            StructuredMimeTypes = ["application/json"],
            UsesBackingStore = true,
        };
        var generationConfiguration = new GenerationConfiguration();
        clientConfiguration.UpdateGenerationConfigurationFromApiClientConfiguration(generationConfiguration, "client");
        Assert.Equal(clientConfiguration.ClientNamespaceName, generationConfiguration.ClientNamespaceName);
        Assert.Equal(GenerationLanguage.CSharp, generationConfiguration.Language);
        Assert.Equal(clientConfiguration.DescriptionLocation, generationConfiguration.OpenAPIFilePath);
        Assert.Equal(clientConfiguration.ExcludeBackwardCompatible, generationConfiguration.ExcludeBackwardCompatible);
        Assert.Equal(clientConfiguration.ExcludePatterns, generationConfiguration.ExcludePatterns);
        Assert.Equal(clientConfiguration.IncludeAdditionalData, generationConfiguration.IncludeAdditionalData);
        Assert.Equal(clientConfiguration.IncludePatterns, generationConfiguration.IncludePatterns);
        Assert.Equal(clientConfiguration.OutputPath, generationConfiguration.OutputPath);
        Assert.Equal(clientConfiguration.StructuredMimeTypes, generationConfiguration.StructuredMimeTypes);
        Assert.Equal(clientConfiguration.UsesBackingStore, generationConfiguration.UsesBackingStore);
        Assert.Empty(generationConfiguration.Serializers);
        Assert.Empty(generationConfiguration.Deserializers);
    }

}
