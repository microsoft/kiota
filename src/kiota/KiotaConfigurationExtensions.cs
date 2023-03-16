using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Configuration;

namespace kiota;

internal static class KiotaConfigurationExtensions
{
    /// <summary>
    /// Binds the configuration to the KiotaConfiguration object
    /// This implementation is a workaround for the fact that Configuration.Bind uses reflection and is not trimmable
    /// <see href="https://github.com/dotnet/runtime/issues/36130"/>
    /// </summary>
    /// <param name="configObject">The configuration object to bind to</param>
    /// <param name="configuration">The configuration to bind from</param>
    public static void BindConfiguration(this KiotaConfiguration configObject, IConfigurationRoot configuration)
    {
        ArgumentNullException.ThrowIfNull(configObject);
        ArgumentNullException.ThrowIfNull(configuration);
        var downloadSection = configuration.GetSection(nameof(configObject.Download));
        configObject.Download.CleanOutput = bool.TryParse(downloadSection[nameof(DownloadConfiguration.CleanOutput)], out var downloadCleanOutput) && downloadCleanOutput;
        configObject.Download.ClearCache = bool.TryParse(downloadSection[nameof(DownloadConfiguration.ClearCache)], out var downloadClearCache) && downloadClearCache;
        configObject.Download.OutputPath = downloadSection[nameof(DownloadConfiguration.OutputPath)] is string value && !string.IsNullOrEmpty(value) ? value : configObject.Download.OutputPath;
        var searchSection = configuration.GetSection(nameof(configObject.Search));
        configObject.Search.ClearCache = bool.TryParse(searchSection[nameof(SearchConfiguration.ClearCache)], out var searchClearCache) && searchClearCache;
        var gitHubSubSection = searchSection.GetSection(nameof(configObject.Search.GitHub));
        configObject.Search.GitHub.ApiBaseUrl = Uri.TryCreate(gitHubSubSection[nameof(GitHubConfiguration.ApiBaseUrl)], new UriCreationOptions(), out var apiBaseUrl) ? apiBaseUrl : configObject.Search.GitHub.ApiBaseUrl;
        configObject.Search.GitHub.AppId = gitHubSubSection[nameof(GitHubConfiguration.AppId)] is string appId && !string.IsNullOrEmpty(appId) ? appId : configObject.Search.GitHub.AppId;
        configObject.Search.GitHub.AppManagement = Uri.TryCreate(gitHubSubSection[nameof(GitHubConfiguration.AppManagement)], new UriCreationOptions(), out var appManagement) ? appManagement : configObject.Search.GitHub.AppManagement;
        configObject.Search.GitHub.BlockListUrl = Uri.TryCreate(gitHubSubSection[nameof(GitHubConfiguration.BlockListUrl)], new UriCreationOptions(), out var blockListUrl) ? blockListUrl : configObject.Search.GitHub.BlockListUrl;

        var languagesSection = configuration.GetSection(nameof(configObject.Languages));
        foreach (var section in languagesSection.GetChildren())
        {
            var lngInfo = new LanguageInformation
            {
                ClientClassName = section[nameof(LanguageInformation.ClientClassName)] ?? string.Empty,
                ClientNamespaceName = section[nameof(LanguageInformation.ClientNamespaceName)] ?? string.Empty,
                DependencyInstallCommand = section[nameof(LanguageInformation.DependencyInstallCommand)] ?? string.Empty,
                MaturityLevel = Enum.TryParse<LanguageMaturityLevel>(section[nameof(LanguageInformation.MaturityLevel)], true, out var ml) ? ml : LanguageMaturityLevel.Experimental,
            };
            section.GetSection(nameof(lngInfo.StructuredMimeTypes)).LoadHashSet(lngInfo.StructuredMimeTypes);
            var dependenciesSection = section.GetSection(nameof(lngInfo.Dependencies));
            foreach (var dependency in dependenciesSection.GetChildren())
            {
                lngInfo.Dependencies.Add(new LanguageDependency
                {
                    Version = dependency[nameof(LanguageDependency.Version)] ?? string.Empty,
                    Name = dependency[nameof(LanguageDependency.Name)] ?? string.Empty,
                });
            }
            configObject.Languages.Add(section.Key, lngInfo);
        }
        var generationSection = configuration.GetSection(nameof(configObject.Generation));
        configObject.Generation.Language = Enum.TryParse<GenerationLanguage>(generationSection[nameof(GenerationConfiguration.Language)], true, out var language) ? language : GenerationLanguage.CSharp;
        configObject.Generation.OpenAPIFilePath = generationSection[nameof(GenerationConfiguration.OpenAPIFilePath)] is string openApiFilePath && !string.IsNullOrEmpty(openApiFilePath) ? openApiFilePath : configObject.Generation.OpenAPIFilePath;
        configObject.Generation.OutputPath = generationSection[nameof(GenerationConfiguration.OutputPath)] is string outputPath && !string.IsNullOrEmpty(outputPath) ? outputPath : configObject.Generation.OutputPath;
        configObject.Generation.ClientClassName = generationSection[nameof(GenerationConfiguration.ClientClassName)] is string clientClassName && !string.IsNullOrEmpty(clientClassName) ? clientClassName : configObject.Generation.ClientClassName;
        configObject.Generation.ClientNamespaceName = generationSection[nameof(GenerationConfiguration.ClientNamespaceName)] is string clientNamespaceName && !string.IsNullOrEmpty(clientNamespaceName) ? clientNamespaceName : configObject.Generation.ClientNamespaceName;
        configObject.Generation.UsesBackingStore = bool.TryParse(generationSection[nameof(GenerationConfiguration.UsesBackingStore)], out var usesBackingStore) && usesBackingStore;
        configObject.Generation.IncludeAdditionalData = bool.TryParse(generationSection[nameof(GenerationConfiguration.IncludeAdditionalData)], out var includeAdditionalData) && includeAdditionalData;
        configObject.Generation.CleanOutput = bool.TryParse(generationSection[nameof(GenerationConfiguration.CleanOutput)], out var cleanOutput) && cleanOutput;
        configObject.Generation.ClearCache = bool.TryParse(generationSection[nameof(GenerationConfiguration.ClearCache)], out var clearCache) && clearCache;
        generationSection.GetSection(nameof(GenerationConfiguration.StructuredMimeTypes)).LoadHashSet(configObject.Generation.StructuredMimeTypes);
        generationSection.GetSection(nameof(GenerationConfiguration.Serializers)).LoadHashSet(configObject.Generation.Serializers);
        generationSection.GetSection(nameof(GenerationConfiguration.Deserializers)).LoadHashSet(configObject.Generation.Deserializers);
        generationSection.GetSection(nameof(GenerationConfiguration.IncludePatterns)).LoadHashSet(configObject.Generation.IncludePatterns);
        generationSection.GetSection(nameof(GenerationConfiguration.ExcludePatterns)).LoadHashSet(configObject.Generation.ExcludePatterns);
        generationSection.GetSection(nameof(GenerationConfiguration.DisabledValidationRules)).LoadHashSet(configObject.Generation.DisabledValidationRules);
    }
    private static void LoadHashSet(this IConfigurationSection section, HashSet<string> hashSet)
    {
        ArgumentNullException.ThrowIfNull(hashSet);
        if (section is null) return;
        var children = section.GetChildren();
        if (children.Any() && hashSet.Any()) hashSet.Clear();
        foreach (var item in children)
        {
            hashSet.Add(item.Key);
        }
    }
}
