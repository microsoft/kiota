using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Kiota.Builder;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;

using Microsoft.Extensions.Logging;

namespace kiota.Handlers;

internal class KiotaGenerationCommandHandler : BaseKiotaCommandHandler
{
    public Option<string> DescriptionOption { get;set; }
    public Option<string> OutputOption { get;set; }
    public Option<GenerationLanguage> LanguageOption { get;set; }
    public Option<string> ClassOption { get;set; }
    public Option<string> NamespaceOption { get;set; }
    public Option<bool> BackingStoreOption { get;set; }
    public Option<bool> AdditionalDataOption { get;set; }
    public Option<List<string>> SerializerOption { get;set; }
    public Option<List<string>> DeserializerOption { get;set; }
    public Option<bool> CleanOutputOption { get;set; }
    public Option<List<string>> StructuredMimeTypesOption { get;set; }
    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        string output = context.ParseResult.GetValueForOption(OutputOption);
        GenerationLanguage language = context.ParseResult.GetValueForOption(LanguageOption);
        string openapi = context.ParseResult.GetValueForOption(DescriptionOption);
        bool backingStore = context.ParseResult.GetValueForOption(BackingStoreOption);
        bool clearCache = context.ParseResult.GetValueForOption(ClearCacheOption);
        bool includeAdditionalData = context.ParseResult.GetValueForOption(AdditionalDataOption);
        string className = context.ParseResult.GetValueForOption(ClassOption);
        string namespaceName = context.ParseResult.GetValueForOption(NamespaceOption);
        List<string> serializer = context.ParseResult.GetValueForOption(SerializerOption);
        List<string> deserializer = context.ParseResult.GetValueForOption(DeserializerOption);
        List<string> includePatterns = context.ParseResult.GetValueForOption(IncludePatternsOption);
        List<string> excludePatterns = context.ParseResult.GetValueForOption(ExcludePatternsOption);
        bool cleanOutput = context.ParseResult.GetValueForOption(CleanOutputOption);
        List<string> structuredMimeTypes = context.ParseResult.GetValueForOption(StructuredMimeTypesOption);
        CancellationToken cancellationToken = (CancellationToken)context.BindingContext.GetService(typeof(CancellationToken));
        AssignIfNotNullOrEmpty(output, (c, s) => c.OutputPath = s);
        AssignIfNotNullOrEmpty(openapi, (c, s) => c.OpenAPIFilePath = s);
        AssignIfNotNullOrEmpty(className, (c, s) => c.ClientClassName = s);
        AssignIfNotNullOrEmpty(namespaceName, (c, s) => c.ClientNamespaceName = s);
        Configuration.Generation.UsesBackingStore = backingStore;
        Configuration.Generation.IncludeAdditionalData = includeAdditionalData;
        Configuration.Generation.Language = language;
        if(serializer?.Any() ?? false)
            Configuration.Generation.Serializers = serializer.Select(x => x.TrimQuotes()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if(deserializer?.Any() ?? false)
            Configuration.Generation.Deserializers = deserializer.Select(x => x.TrimQuotes()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if(includePatterns?.Any() ?? false)
            Configuration.Generation.IncludePatterns = includePatterns.Select(x => x.TrimQuotes()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if(excludePatterns?.Any() ?? false)
            Configuration.Generation.ExcludePatterns = excludePatterns.Select(x => x.TrimQuotes()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if(structuredMimeTypes?.Any() ?? false)
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
                await new KiotaBuilder(logger, Configuration.Generation).GenerateClientAsync(cancellationToken);
                return 0;
            } catch (Exception ex) {
    #if DEBUG
                logger.LogCritical(ex, "error generating the SDK: {exceptionMessage}", ex.Message);
                throw; // so debug tools go straight to the source of the exception when attached
    #else
                logger.LogCritical("error generating the SDK: {exceptionMessage}", ex.Message);
                return 1;
    #endif
            }
        }
    }
    private void AssignIfNotNullOrEmpty(string input, Action<GenerationConfiguration, string> assignment) {
        if (!string.IsNullOrEmpty(input))
            assignment.Invoke(Configuration.Generation, input);
    }
    public Option<List<string>> IncludePatternsOption { get; set; }
    public Option<List<string>> ExcludePatternsOption { get; set; }
    public Option<bool> ClearCacheOption { get; set; }
    private static string GetAbsolutePath(string source) => Path.IsPathRooted(source) || (source?.StartsWith("http") ?? false) ? source : NormalizeSlashesInPath(Path.Combine(Directory.GetCurrentDirectory(), source));
}
