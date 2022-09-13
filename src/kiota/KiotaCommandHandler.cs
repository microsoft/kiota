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
using Kiota.Builder.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace kiota;

internal class KiotaCommandHandler : ICommandHandler
{
    public Option<string> DescriptionOption { get;set; }
    public Option<string> OutputOption { get;set; }
    public Option<GenerationLanguage> LanguageOption { get;set; }
    public Option<string> ClassOption { get;set; }
    public Option<string> NamespaceOption { get;set; }
    public Option<LogLevel> LogLevelOption { get;set; }
    public Option<bool> BackingStoreOption { get;set; }
    public Option<bool> AdditionalDataOption { get;set; }
    public Option<List<string>> SerializerOption { get;set; }
    public Option<List<string>> DeserializerOption { get;set; }
    public Option<bool> CleanOutputOption { get;set; }
    public Option<List<string>> StructuredMimeTypesOption { get;set; }
    public int Invoke(InvocationContext context)
    {
        return InvokeAsync(context).GetAwaiter().GetResult();
    }
    public async Task<int> InvokeAsync(InvocationContext context)
    {
        string output = context.ParseResult.GetValueForOption(OutputOption);
        GenerationLanguage language = context.ParseResult.GetValueForOption(LanguageOption);
        string openapi = context.ParseResult.GetValueForOption(DescriptionOption);
        bool backingStore = context.ParseResult.GetValueForOption(BackingStoreOption);
        bool includeAdditionalData = context.ParseResult.GetValueForOption(AdditionalDataOption);
        string className = context.ParseResult.GetValueForOption(ClassOption);
        LogLevel logLevel = context.ParseResult.GetValueForOption(LogLevelOption);
        string namespaceName = context.ParseResult.GetValueForOption(NamespaceOption);
        List<string> serializer = context.ParseResult.GetValueForOption(SerializerOption);
        List<string> deserializer = context.ParseResult.GetValueForOption(DeserializerOption);
        bool cleanOutput = context.ParseResult.GetValueForOption(CleanOutputOption);
        List<string> structuredMimeTypes = context.ParseResult.GetValueForOption(StructuredMimeTypesOption);
        CancellationToken cancellationToken = (CancellationToken)context.BindingContext.GetService(typeof(CancellationToken));
        AssignIfNotNullOrEmpty(output, (c, s) => c.OutputPath = s);
        AssignIfNotNullOrEmpty(openapi, (c, s) => c.OpenAPIFilePath = s);
        AssignIfNotNullOrEmpty(className, (c, s) => c.ClientClassName = s);
        AssignIfNotNullOrEmpty(namespaceName, (c, s) => c.ClientNamespaceName = s);
        Configuration.UsesBackingStore = backingStore;
        Configuration.IncludeAdditionalData = includeAdditionalData;
        Configuration.Language = language;
        if(serializer?.Any() ?? false)
            Configuration.Serializers = serializer.Select(x => x.TrimQuotes()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if(deserializer?.Any() ?? false)
            Configuration.Deserializers = deserializer.Select(x => x.TrimQuotes()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if(structuredMimeTypes?.Any() ?? false)
            Configuration.StructuredMimeTypes = structuredMimeTypes.SelectMany(x => x.Split(new[] {' '}))
                                                            .Select(x => x.TrimQuotes())
                                                            .ToHashSet(StringComparer.OrdinalIgnoreCase);

#if DEBUG
        logLevel = logLevel > LogLevel.Debug ? LogLevel.Debug : logLevel;
#endif

        Configuration.OpenAPIFilePath = GetAbsolutePath(Configuration.OpenAPIFilePath);
        Configuration.OutputPath = GetAbsolutePath(Configuration.OutputPath);
        Configuration.CleanOutput = cleanOutput;

        using var loggerFactory = LoggerFactory.Create(builder => {
            builder
                .AddConsole()
#if DEBUG
                .AddDebug()
#endif
                .SetMinimumLevel(logLevel);
        });
        var logger = loggerFactory.CreateLogger<KiotaBuilder>();

        logger.LogTrace("configuration: {configuration}", JsonSerializer.Serialize(Configuration));

        try {
            await new KiotaBuilder(logger, Configuration).GenerateSDK(cancellationToken);
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
    private void AssignIfNotNullOrEmpty(string input, Action<GenerationConfiguration, string> assignment) {
        if (!string.IsNullOrEmpty(input))
            assignment.Invoke(Configuration, input);
    }
    private GenerationConfiguration Configuration { get => ConfigurationFactory.Value; }
    private readonly Lazy<GenerationConfiguration> ConfigurationFactory = new (() => {
        var builder = new ConfigurationBuilder();
        var configuration = builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables(prefix: "KIOTA_")
                .Build();
        var configObject = new GenerationConfiguration();
        configuration.Bind(configObject);
        return configObject;
    });
    private static string GetAbsolutePath(string source) => Path.IsPathRooted(source) || source.StartsWith("http") ? source : Path.Combine(Directory.GetCurrentDirectory(), source);
}
