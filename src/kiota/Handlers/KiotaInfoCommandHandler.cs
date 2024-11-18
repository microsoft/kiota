﻿using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;
using Kiota.Builder;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Writers;

namespace kiota.Handlers;
internal class KiotaInfoCommandHandler : KiotaSearchBasedCommandHandler
{
    public required Option<string> DescriptionOption
    {
        get; init;
    }
    public required Option<bool> ClearCacheOption
    {
        get; init;
    }
    public required Option<string> SearchTermOption
    {
        get; init;
    }
    public required Option<string> VersionOption
    {
        get; init;
    }
    public required Option<GenerationLanguage?> GenerationLanguage
    {
        get; init;
    }
    public required Option<string> ManifestOption
    {
        get; init;
    }
    public required Option<bool> JsonOption
    {
        get; init;
    }
    public required Option<DependencyType[]> DependencyTypesOption
    {
        get; init;
    }

    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        string openapi = context.ParseResult.GetValueForOption(DescriptionOption) ?? string.Empty;
        string manifest = context.ParseResult.GetValueForOption(ManifestOption) ?? string.Empty;
        bool clearCache = context.ParseResult.GetValueForOption(ClearCacheOption);
        string searchTerm = context.ParseResult.GetValueForOption(SearchTermOption) ?? string.Empty;
        string version = context.ParseResult.GetValueForOption(VersionOption) ?? string.Empty;
        bool json = context.ParseResult.GetValueForOption(JsonOption);
        DependencyType[] dependencyTypes = context.ParseResult.GetValueForOption(DependencyTypesOption) ?? [];
        GenerationLanguage? language = context.ParseResult.GetValueForOption(GenerationLanguage);
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;
        var (loggerFactory, logger) = GetLoggerAndFactory<KiotaBuilder>(context);
        Configuration.Search.ClearCache = clearCache;
        using (loggerFactory)
        {
            await CheckForNewVersionAsync(logger, cancellationToken).ConfigureAwait(false);
            if (!language.HasValue)
            {
                ShowLanguagesTable();
                DisplayInfoAdvancedHint();
                return 0;
            }

            var (searchResultDescription, statusCode) = await GetDescriptionFromSearchAsync(openapi, manifest, searchTerm, version, loggerFactory, logger, cancellationToken);
            if (statusCode.HasValue)
            {
                return statusCode.Value;
            }
            if (!string.IsNullOrEmpty(searchResultDescription))
            {
                openapi = searchResultDescription;
            }

            Configuration.Generation.OpenAPIFilePath = GetAbsolutePath(openapi);
            Configuration.Generation.ClearCache = clearCache;
            Configuration.Generation.Language = language.Value;

            var instructions = Configuration.Languages;
            if (!string.IsNullOrEmpty(openapi))
                try
                {
                    var builder = new KiotaBuilder(logger, Configuration.Generation, httpClient);
                    var result = await builder.GetLanguagesInformationAsync(cancellationToken);
                    if (result != null)
                        instructions = result;
                }
                catch (Exception ex)
                {
#if DEBUG
                    logger.LogCritical(ex, "error getting information from the description: {exceptionMessage}", ex.Message);
                    throw; // so debug tools go straight to the source of the exception when attached
#else
                    logger.LogCritical("error getting information from the description: {exceptionMessage}", ex.Message);
                    return 1;
#endif
                }
            ShowLanguageInformation(language.Value, instructions, json, dependencyTypes);
            return 0;
        }
    }
    private void ShowLanguagesTable()
    {
        var defaultInformation = Configuration.Languages;
        var view = new TableView<KeyValuePair<string, LanguageInformation>>()
        {
            Items = defaultInformation.OrderBy(static x => x.Key).Select(static x => x).ToList(),
        };
        view.AddColumn(static x => x.Key, "Language");
        view.AddColumn(static x => x.Value.MaturityLevel.ToString(), "Maturity Level");
        var console = new SystemConsole();
        using var terminal = new SystemConsoleTerminal(console);
        var layout = new StackLayoutView { view };
        console.Append(layout);
    }
    private void ShowLanguageInformation(GenerationLanguage language, LanguagesInformation informationSource, bool json, DependencyType[] dependencyTypes)
    {
        if (informationSource.TryGetValue(language.ToString(), out var languageInformation))
        {
            if (!json)
            {
                DisplayInfo($"The language {language} is currently in {languageInformation.MaturityLevel} maturity level.",
                            "After generating code for this language, you need to install the following packages:");
                var orderedDependencies = languageInformation.Dependencies.OrderBy(static x => x.Name).Select(static x => x).ToList();
                var filteredDependencies = (dependencyTypes.ToHashSet(), orderedDependencies.Any(static x => x.DependencyType is DependencyType.Bundle)) switch
                {
                    //if the user requested a specific type, we filter the dependencies
                    ({ Count: > 0 }, _) => orderedDependencies.Where(x => x.DependencyType is null || dependencyTypes.Contains(x.DependencyType.Value)).ToList(),
                    //otherwise we display only the bundle dependencies
                    (_, true) => orderedDependencies.Where(static x => x.DependencyType is DependencyType.Bundle or DependencyType.Authentication or DependencyType.Additional).ToList(),
                    //otherwise we display all dependencies
                    _ => orderedDependencies
                };
                var view = new TableView<LanguageDependency>()
                {
                    Items = filteredDependencies,
                };
                view.AddColumn(static x => x.Name, "Package Name");
                view.AddColumn(static x => x.Version, "Version");
                if (orderedDependencies.Any(static x => x.DependencyType is not null))
                    view.AddColumn(static x => x.DependencyType?.ToString(), "Type");
                var console = new SystemConsole();
                using var terminal = new SystemConsoleTerminal(console);
                var layout = new StackLayoutView { view };
                console.Append(layout);
                DisplayDependenciesHint(language);
                DisplayInstallHint(languageInformation, filteredDependencies);
            }
            else
            {
                using TextWriter sWriter = new StringWriter();
                OpenApiJsonWriter writer = new(sWriter);
                languageInformation.SerializeAsV3(writer);
                DisplayInfo(sWriter.ToString()!);
            }
        }
        else
        {
            DisplayInfo($"No information for {language}.");
        }
    }
}
