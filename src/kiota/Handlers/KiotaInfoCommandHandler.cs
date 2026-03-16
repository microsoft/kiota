using System.CommandLine;
using System.Diagnostics;
using kiota.Extension;
using kiota.Telemetry;
using Kiota.Builder;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

namespace kiota.Handlers;

internal class
    KiotaInfoCommandHandler : KiotaSearchBasedCommandHandler
{
    private readonly KeyValuePair<string, object?>[] _commonTags =
    [
        new(TelemetryLabels.TagCommandName, "info"),
        new(TelemetryLabels.TagCommandRevision, 1)
    ];
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

    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        // Span start time
        Stopwatch? stopwatch = Stopwatch.StartNew();
        var startTime = DateTimeOffset.UtcNow;
        // Get options
        string? openapi0 = parseResult.GetValue(DescriptionOption);
        string manifest = parseResult.GetValue(ManifestOption).OrEmpty();
        bool clearCache = parseResult.GetValue(ClearCacheOption);
        string? searchTerm0 = parseResult.GetValue(SearchTermOption);
        string? version0 = parseResult.GetValue(VersionOption);
        bool json = parseResult.GetValue(JsonOption);
        DependencyType[]? dependencyTypes0 = parseResult.GetValue(DependencyTypesOption);
        GenerationLanguage? language = parseResult.GetValue(GenerationLanguage);
        var logLevel = parseResult.GetResult(LogLevelOption)?.GetValueOrDefault<LogLevel>() as LogLevel?;
        var instrumentation = ServiceProvider?.GetService<Instrumentation>();
        var activitySource = instrumentation?.ActivitySource;

        CreateTelemetryTags(activitySource, searchTerm0, openapi0, version0, language, clearCache,
            logLevel, out var tags);
        // Start span
        using var invokeActivity = activitySource?.StartActivity(ActivityKind.Internal, name: TelemetryLabels.SpanInfoCommand,
            startTime: startTime,
            tags: _commonTags.ConcatNullable(tags)?.Concat(Telemetry.Telemetry.GetThreadTags()));
        // Command duration meter
        var meterRuntime = instrumentation?.CreateCommandDurationHistogram();
        if (meterRuntime is null) stopwatch = null;
        // Add this run to the command execution counter
        var tl = new TagList(_commonTags.AsSpan()).AddAll(tags.OrEmpty());
        instrumentation?.CreateCommandExecutionCounter().Add(1, tl);

        string openapi = openapi0.OrEmpty();
        string searchTerm = searchTerm0.OrEmpty();
        string version = version0.OrEmpty();
        DependencyType[] dependencyTypes = dependencyTypes0.OrEmpty();
        var (loggerFactory, logger) = GetLoggerAndFactory<KiotaBuilder>(parseResult);
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
            {
                try
                {
                    var builder = new KiotaBuilder(logger, Configuration.Generation, httpClient);
                    var result = await builder.GetLanguagesInformationAsync(cancellationToken);
                    if (result != null)
                        instructions = result;
                }
                catch (Exception ex)
                {
                    invokeActivity?.SetStatus(ActivityStatusCode.Error);
                    invokeActivity?.AddException(ex);
#if DEBUG
                    logger.LogCritical(ex, "error getting information from the description: {exceptionMessage}",
                        ex.Message);
                    throw; // so debug tools go straight to the source of the exception when attached
#else
                    logger.LogCritical("error getting information from the description: {exceptionMessage}", ex.Message);
                    return 1;
#endif
                }
                finally
                {
                    if (stopwatch is not null) meterRuntime?.Record(stopwatch.Elapsed.TotalSeconds, tl);
                }
            }
            ShowLanguageInformation(language.Value, instructions, json, dependencyTypes);
            return 0;
        }
    }
    private void ShowLanguagesTable()
    {
        var defaultInformation = Configuration.Languages;
        var headers = new[] { "Language", "Maturity Level", "Support Experience" };
        var rows = defaultInformation.OrderBy(static x => x.Key)
            .Select(static x => new[] { x.Key, x.Value.MaturityLevel.ToString(), x.Value.SupportExperience.ToString() })
            .ToList();
        DisplayTable(headers, rows);
    }
    private void ShowLanguageInformation(GenerationLanguage language, LanguagesInformation informationSource, bool json, DependencyType[] dependencyTypes)
    {
        if (informationSource.TryGetValue(language.ToString(), out var languageInformation))
        {
            if (!json)
            {
                DisplayInfo($"The language {language} is currently in {languageInformation.MaturityLevel} maturity level.",
                            $"The support experience is provided by {languageInformation.SupportExperience}.",
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
                var headers = new[] { "Package Name", "Version" };
                if (orderedDependencies.Any(static x => x.DependencyType is not null))
                    headers = [.. headers, "Type"];
                var rows = filteredDependencies.Select(x => new[] { x.Name, x.Version, x.DependencyType?.ToString() ?? "" }).ToList();
                DisplayTable(headers, rows);
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

    private static void CreateTelemetryTags(ActivitySource? activitySource, string? searchTerm, string? openapi,
        string? version, GenerationLanguage? language, bool clearCache, LogLevel? logLevel,
        out List<KeyValuePair<string, object?>>? tags)
    {
        // set up telemetry tags
        const string redacted = TelemetryLabels.RedactedValuePlaceholder;
        tags = activitySource?.HasListeners() == true ? new List<KeyValuePair<string, object?>>(7)
        {
            new(TelemetryLabels.TagCommandSource, TelemetryLabels.CommandSourceCliValue),
            new($"{TelemetryLabels.TagCommandParams}.clear_cache", clearCache),
        } : null;
        if (searchTerm is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.search_term", redacted));
        if (openapi is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.openapi", redacted));
        if (version is not null) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.version", redacted));
        if (language is { } lang) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.language", lang.ToString("G")));
        if (logLevel is { } ll) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.log_level", ll.ToString("G")));
    }
}
