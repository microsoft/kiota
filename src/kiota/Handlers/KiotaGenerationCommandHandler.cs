using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Kiota.Builder;
using Kiota.Builder.Extensions;

using Microsoft.Extensions.Logging;

namespace kiota.Handlers;

internal class KiotaGenerationCommandHandler : BaseKiotaCommandHandler
{
    public required Option<string> DescriptionOption { get;init; }
    public required Option<string> OutputOption { get;init; }
    public required Option<GenerationLanguage> LanguageOption { get;init; }
    public required Option<string> ClassOption { get;init; }
    public required Option<string> NamespaceOption { get;init; }
    public required Option<bool> BackingStoreOption { get;init; }
    public required Option<bool> AdditionalDataOption { get;init; }
    public required Option<List<string>> SerializerOption { get;init; }
    public required Option<List<string>> DeserializerOption { get;init; }
    public required Option<bool> CleanOutputOption { get;init; }
    public required Option<List<string>> StructuredMimeTypesOption { get;init; }
    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        string output = context.ParseResult.GetValueForOption(OutputOption) ?? string.Empty;
        GenerationLanguage language = context.ParseResult.GetValueForOption(LanguageOption);
        string openapi = context.ParseResult.GetValueForOption(DescriptionOption) ?? string.Empty;
        bool backingStore = context.ParseResult.GetValueForOption(BackingStoreOption);
        bool clearCache = context.ParseResult.GetValueForOption(ClearCacheOption);
        bool includeAdditionalData = context.ParseResult.GetValueForOption(AdditionalDataOption);
        string className = context.ParseResult.GetValueForOption(ClassOption) ?? string.Empty;
        string namespaceName = context.ParseResult.GetValueForOption(NamespaceOption) ?? string.Empty;
        List<string> serializer = context.ParseResult.GetValueForOption(SerializerOption) ?? new List<string>();
        List<string> deserializer = context.ParseResult.GetValueForOption(DeserializerOption) ?? new List<string>();
        List<string> includePatterns = context.ParseResult.GetValueForOption(IncludePatternsOption) ?? new List<string>();
        List<string> excludePatterns = context.ParseResult.GetValueForOption(ExcludePatternsOption) ?? new List<string>();
        bool cleanOutput = context.ParseResult.GetValueForOption(CleanOutputOption);
        List<string> structuredMimeTypes = context.ParseResult.GetValueForOption(StructuredMimeTypesOption) ?? new List<string>();
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;
        AssignIfNotNullOrEmpty(output, (c, s) => c.OutputPath = s);
        AssignIfNotNullOrEmpty(openapi, (c, s) => c.OpenAPIFilePath = s);
        AssignIfNotNullOrEmpty(className, (c, s) => c.ClientClassName = s);
        AssignIfNotNullOrEmpty(namespaceName, (c, s) => c.ClientNamespaceName = s);
        Configuration.Generation.UsesBackingStore = backingStore;
        Configuration.Generation.IncludeAdditionalData = includeAdditionalData;
        Configuration.Generation.Language = language;
        if(serializer.Any())
            Configuration.Generation.Serializers = serializer.Select(x => x.TrimQuotes()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if(deserializer.Any())
            Configuration.Generation.Deserializers = deserializer.Select(x => x.TrimQuotes()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if(includePatterns.Any())
            Configuration.Generation.IncludePatterns = includePatterns.Select(x => x.TrimQuotes()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if(excludePatterns.Any())
            Configuration.Generation.ExcludePatterns = excludePatterns.Select(x => x.TrimQuotes()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if(structuredMimeTypes.Any())
            Configuration.Generation.StructuredMimeTypes = structuredMimeTypes.SelectMany(x => x.Split(new[] {' '}))
                                                            .Select(x => x.TrimQuotes())
                                                            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Configuration.Generation.OpenAPIFilePath = GetAbsolutePath(Configuration.Generation.OpenAPIFilePath);
        Configuration.Generation.OutputPath = NormalizeSlashesInPath(GetAbsolutePath(Configuration.Generation.OutputPath));
        Configuration.Generation.CleanOutput = cleanOutput;
        Configuration.Generation.ClearCache = clearCache;

        var (loggerFactory, logger) = GetLoggerAndFactory<KiotaBuilder>(context);
        using (loggerFactory) {
            logger.LogTrace("configuration: {configuration}", JsonSerializer.Serialize(Configuration));

            try {
                var result = await new KiotaBuilder(logger, Configuration.Generation, httpClient).GenerateClientAsync(cancellationToken);
                if (result)
                    DisplaySuccess("Generation completed successfully");
                else {
                    DisplaySuccess("Generation skipped as no changes were detected");
                    DisplayCleanHint("generate");
                }
                DisplayInfoHint(language, Configuration.Generation.OpenAPIFilePath);
                DisplayGenerateAdvancedHint(includePatterns, excludePatterns, Configuration.Generation.OpenAPIFilePath);
                return 0;
            } catch (Exception ex) {
    #if DEBUG
                logger.LogCritical(ex, "error generating the client: {exceptionMessage}", ex.Message);
                throw; // so debug tools go straight to the source of the exception when attached
    #else
                logger.LogCritical("error generating the client: {exceptionMessage}", ex.Message);
                return 1;
    #endif
            }
        }
    }
    public required Option<List<string>> IncludePatternsOption { get; init; }
    public required Option<List<string>> ExcludePatternsOption { get; init; }
    public required Option<bool> ClearCacheOption { get; init; }
}
