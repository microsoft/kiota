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
        configObject.Download.CleanOutput = bool.TryParse(configuration[$"{nameof(configObject.Download)}:{nameof(DownloadConfiguration.CleanOutput)}"], out var downloadCleanOutput) && downloadCleanOutput;
        configObject.Download.ClearCache = bool.TryParse(configuration[$"{nameof(configObject.Download)}:{nameof(DownloadConfiguration.ClearCache)}"], out var downloadClearCache) && downloadClearCache;
        configObject.Download.OutputPath = configuration[$"{nameof(configObject.Download)}:{nameof(DownloadConfiguration.OutputPath)}"] is string value && !string.IsNullOrEmpty(value) ? value : configObject.Download.OutputPath;
        configObject.Search.ClearCache = bool.TryParse(configuration[$"{nameof(configObject.Search)}:{nameof(SearchConfiguration.ClearCache)}"], out var searchClearCache) && searchClearCache;
        configObject.Search.GitHub.ApiBaseUrl = Uri.TryCreate(configuration[$"{nameof(configObject.Search)}:{nameof(SearchConfiguration.GitHub)}:{nameof(GitHubConfiguration.ApiBaseUrl)}"], new UriCreationOptions(), out var apiBaseUrl) ? apiBaseUrl : configObject.Search.GitHub.ApiBaseUrl;
        configObject.Search.GitHub.AppId = configuration[$"{nameof(configObject.Search)}:{nameof(SearchConfiguration.GitHub)}:{nameof(GitHubConfiguration.AppId)}"] is string appId && !string.IsNullOrEmpty(appId) ? appId : configObject.Search.GitHub.AppId;
        configObject.Search.GitHub.AppManagement = Uri.TryCreate(configuration[$"{nameof(configObject.Search)}:{nameof(SearchConfiguration.GitHub)}:{nameof(GitHubConfiguration.AppManagement)}"], new UriCreationOptions(), out var appManagement) ? appManagement : configObject.Search.GitHub.AppManagement;
        configObject.Search.GitHub.BlockListUrl = Uri.TryCreate(configuration[$"{nameof(configObject.Search)}:{nameof(SearchConfiguration.GitHub)}:{nameof(GitHubConfiguration.BlockListUrl)}"], new UriCreationOptions(), out var blockListUrl) ? blockListUrl : configObject.Search.GitHub.BlockListUrl;

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
        configObject.Generation.Language = Enum.TryParse<GenerationLanguage>(configuration[$"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.Language)}"], true, out var language) ? language : GenerationLanguage.CSharp;
        configObject.Generation.OpenAPIFilePath = configuration[$"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.OpenAPIFilePath)}"] is string openApiFilePath && !string.IsNullOrEmpty(openApiFilePath) ? openApiFilePath : configObject.Generation.OpenAPIFilePath;
        configObject.Generation.OutputPath = configuration[$"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.OutputPath)}"] is string outputPath && !string.IsNullOrEmpty(outputPath) ? outputPath : configObject.Generation.OutputPath;
        configObject.Generation.ClientClassName = configuration[$"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.ClientClassName)}"] is string clientClassName && !string.IsNullOrEmpty(clientClassName) ? clientClassName : configObject.Generation.ClientClassName;
        configObject.Generation.ClientNamespaceName = configuration[$"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.ClientNamespaceName)}"] is string clientNamespaceName && !string.IsNullOrEmpty(clientNamespaceName) ? clientNamespaceName : configObject.Generation.ClientNamespaceName;
        configObject.Generation.UsesBackingStore = bool.TryParse(configuration[$"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.UsesBackingStore)}"], out var usesBackingStore) && usesBackingStore;
        configObject.Generation.IncludeAdditionalData = bool.TryParse(configuration[$"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.IncludeAdditionalData)}"], out var includeAdditionalData) && includeAdditionalData;
        configObject.Generation.CleanOutput = bool.TryParse(configuration[$"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.CleanOutput)}"], out var cleanOutput) && cleanOutput;
        configObject.Generation.ClearCache = bool.TryParse(configuration[$"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.ClearCache)}"], out var clearCache) && clearCache;
        configuration.GetSection($"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.StructuredMimeTypes)}").LoadHashSet(configObject.Generation.StructuredMimeTypes);
        configuration.GetSection($"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.Serializers)}").LoadHashSet(configObject.Generation.Serializers);
        configuration.GetSection($"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.Deserializers)}").LoadHashSet(configObject.Generation.Deserializers);
        configuration.GetSection($"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.IncludePatterns)}").LoadHashSet(configObject.Generation.IncludePatterns);
        configuration.GetSection($"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.ExcludePatterns)}").LoadHashSet(configObject.Generation.ExcludePatterns);
        configuration.GetSection($"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.DisabledValidationRules)}").LoadHashSet(configObject.Generation.DisabledValidationRules);
    }
    private static void LoadHashSet(this IConfigurationSection section, HashSet<string> hashSet)
    {
        ArgumentNullException.ThrowIfNull(hashSet);
        if (section is null) return;
        var children = section.GetChildren();
        if (children.Any() && hashSet.Any()) hashSet.Clear();
        foreach (var item in children)
        {
            if (section[item.Key] is string value && !string.IsNullOrEmpty(value))
                hashSet.Add(value);
        }
    }
}
