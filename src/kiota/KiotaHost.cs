using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text.RegularExpressions;
using kiota.Handlers;
using kiota.Rpc;
using Kiota.Builder;
using Kiota.Builder.Configuration;
using Kiota.Builder.Validation;
using Microsoft.Extensions.Logging;

namespace kiota;
public static partial class KiotaHost
{
    internal static readonly Lazy<bool> IsConfigPreviewEnabled = new(() => bool.TryParse(Environment.GetEnvironmentVariable("KIOTA_CONFIG_PREVIEW"), out var isPreviewEnabled) && isPreviewEnabled);
    public static RootCommand GetRootCommand()
    {
        var rootCommand = new RootCommand();
        if (!IsConfigPreviewEnabled.Value)
            rootCommand.AddCommand(GetGenerateCommand());
        rootCommand.AddCommand(GetSearchCommand());
        rootCommand.AddCommand(GetDownloadCommand());
        rootCommand.AddCommand(GetShowCommand());
        rootCommand.AddCommand(GetInfoCommand());
        if (!IsConfigPreviewEnabled.Value)
            rootCommand.AddCommand(GetUpdateCommand());
        rootCommand.AddCommand(GetLoginCommand());
        rootCommand.AddCommand(GetLogoutCommand());
        rootCommand.AddCommand(GetRpcCommand());
        if (IsConfigPreviewEnabled.Value)
        {
            rootCommand.AddCommand(KiotaConfigCommands.GetConfigNodeCommand());
            rootCommand.AddCommand(KiotaClientCommands.GetClientNodeCommand());
        }
        return rootCommand;
    }
    private static Command GetGitHubLoginCommand()
    {
        var githubLoginCommand = new Command("github", "Logs in to GitHub.");
        githubLoginCommand.AddCommand(GetGitHubDeviceLoginCommand());
        githubLoginCommand.AddCommand(GetGitHubPatLoginCommand());
        return githubLoginCommand;
    }
    private static Command GetGitHubDeviceLoginCommand()
    {
        var logLevelOption = GetLogLevelOption();
        var deviceLoginCommand = new Command("device", "Logs in to GitHub using a device code flow.")
        {
            logLevelOption,
        };
        deviceLoginCommand.Handler = new KiotaGitHubDeviceLoginCommandHandler
        {
            LogLevelOption = logLevelOption,
        };
        return deviceLoginCommand;
    }
    private static Command GetGitHubPatLoginCommand()
    {
        var logLevelOption = GetLogLevelOption();
        var patOption = new Option<string>("--pat", "The personal access token to use to authenticate to GitHub.")
        {
            IsRequired = true
        };
        var deviceLoginCommand = new Command("pat", "Logs in to GitHub using a Personal Access Token.")
        {
            logLevelOption,
            patOption,
        };
        deviceLoginCommand.Handler = new KiotaGitHubPatLoginCommandHandler
        {
            LogLevelOption = logLevelOption,
            PatOption = patOption,
        };
        return deviceLoginCommand;
    }
    private static Command GetGitHubLogoutCommand()
    {
        var logLevelOption = GetLogLevelOption();
        var githubLogoutCommand = new Command("github", "Logs out of GitHub.") {
            logLevelOption,
        };
        githubLogoutCommand.Handler = new KiotaGitHubLogoutCommandHandler
        {
            LogLevelOption = logLevelOption,
        };
        return githubLogoutCommand;
    }
    private static Command GetLoginCommand()
    {
        var loginCommand = new Command("login", "Logs in to the Kiota registries so search/download/show/generate commands can access private API definitions.");
        loginCommand.AddCommand(GetGitHubLoginCommand());
        return loginCommand;
    }
    private static Command GetLogoutCommand()
    {
        var loginCommand = new Command("logout", "Logs out of Kiota registries.");
        loginCommand.AddCommand(GetGitHubLogoutCommand());
        return loginCommand;
    }
    private static Command GetInfoCommand()
    {
        var defaultGenerationConfiguration = new GenerationConfiguration();
        var descriptionOption = GetDescriptionOption(defaultGenerationConfiguration.OpenAPIFilePath);
        var manifestOption = GetManifestOption(defaultGenerationConfiguration.ApiManifestPath);
        var versionOption = GetVersionOption();
        var logLevelOption = GetLogLevelOption();
        var clearCacheOption = GetClearCacheOption(defaultGenerationConfiguration.ClearCache);
        var searchTermOption = GetSearchKeyOption();
        var languageOption = new Option<GenerationLanguage?>("--language", "The target language for the dependencies instructions.");
        var jsonOption = new Option<bool>("--json", "Generate a plain and machine-parsable json output.");
        languageOption.AddAlias("-l");
        AddEnumValidator(languageOption, "language");
        var infoCommand = new Command("info", "Displays information about the languages supported by kiota and dependencies to add in your project.") {
            descriptionOption,
            manifestOption,
            versionOption,
            logLevelOption,
            clearCacheOption,
            searchTermOption,
            languageOption,
            jsonOption,
        };
        infoCommand.Handler = new KiotaInfoCommandHandler
        {
            DescriptionOption = descriptionOption,
            ManifestOption = manifestOption,
            VersionOption = versionOption,
            LogLevelOption = logLevelOption,
            ClearCacheOption = clearCacheOption,
            SearchTermOption = searchTermOption,
            GenerationLanguage = languageOption,
            JsonOption = jsonOption,
        };
        return infoCommand;
    }
    private static Option<string> GetSearchKeyOption()
    {
        var option = new Option<string>("--search-key", () => string.Empty, "The API search key to display the description for. Use the search command to get the key.");
        option.AddAlias("-k");
        return option;
    }
    private static Command GetShowCommand()
    {
        var defaultGenerationConfiguration = new GenerationConfiguration();
        var descriptionOption = GetDescriptionOption(defaultGenerationConfiguration.OpenAPIFilePath);
        var manifestOption = GetManifestOption(defaultGenerationConfiguration.ApiManifestPath);

        var versionOption = GetVersionOption();
        var logLevelOption = GetLogLevelOption();
        var (includePatterns, excludePatterns) = GetIncludeAndExcludeOptions(defaultGenerationConfiguration.IncludePatterns, defaultGenerationConfiguration.ExcludePatterns);
        var clearCacheOption = GetClearCacheOption(defaultGenerationConfiguration.ClearCache);
        var searchTermOption = GetSearchKeyOption();
        var maxDepthOption = new Option<uint>("--max-depth", () => 5, "The maximum depth of the tree to display");
        maxDepthOption.AddAlias("--m-d");
        var displayCommand = new Command("show", "Displays the API tree in a given description."){
            searchTermOption,
            logLevelOption,
            versionOption,
            descriptionOption,
            manifestOption,
            maxDepthOption,
            includePatterns,
            excludePatterns,
            clearCacheOption,
        };
        displayCommand.Handler = new KiotaShowCommandHandler
        {
            SearchTermOption = searchTermOption,
            LogLevelOption = logLevelOption,
            VersionOption = versionOption,
            DescriptionOption = descriptionOption,
            ManifestOption = manifestOption,
            MaxDepthOption = maxDepthOption,
            IncludePatternsOption = includePatterns,
            ExcludePatternsOption = excludePatterns,
            ClearCacheOption = clearCacheOption,
        };
        return displayCommand;
    }
    internal static Command GetRpcCommand()
    {
        var modeOption = new Option<RpcMode>("--mode", "Whether the RPC server should use stdin/stdout or a named pipe.");
        modeOption.AddAlias("-m");
        modeOption.SetDefaultValue(RpcMode.Stdio);
        var pipeNameOption = new Option<string>("--pipe-name", "The name of the named pipe to use for the RPC server.");
        pipeNameOption.AddAlias("-p");
        pipeNameOption.SetDefaultValue("KiotaJsonRpc");
        var commandHandler = new KiotaRpcCommandHandler
        {
            ModeOption = modeOption,
            PipeNameOption = pipeNameOption,
        };
        var command = new Command("rpc", "WARNING EXPERIMENTAL: Starts a kiota as a JSON-RPC server.") {
            modeOption,
            pipeNameOption,
        };
        command.Handler = commandHandler;
        return command;
    }
    private static Command GetDownloadCommand()
    {
        var keyArgument = new Argument<string>("key", "The search result key to download the description for. Use the search command to get the key.");
        var defaultConfiguration = new DownloadConfiguration();

        var logLevelOption = GetLogLevelOption();

        var clearCacheOption = GetClearCacheOption(defaultConfiguration.ClearCache);

        var cleanOutputOption = GetCleanOutputOption(defaultConfiguration.CleanOutput);

        var versionOption = GetVersionOption();

        var outputOption = GetOutputPathOption(defaultConfiguration.OutputPath);

        var searchCommand = new Command("download", "Downloads an OpenAPI description from multiple registries."){
            keyArgument,
            logLevelOption,
            clearCacheOption,
            versionOption,
            cleanOutputOption,
            outputOption,
        };
        searchCommand.Handler = new KiotaDownloadCommandHandler
        {
            SearchTermArgument = keyArgument,
            LogLevelOption = logLevelOption,
            ClearCacheOption = clearCacheOption,
            VersionOption = versionOption,
            CleanOutputOption = cleanOutputOption,
            OutputPathOption = outputOption,
        };
        return searchCommand;
    }
    private static Option<string> GetVersionOption()
    {
        var versionOption = new Option<string>("--version", () => string.Empty, "The version of the OpenAPI document to use.");
        versionOption.AddAlias("-v");
        return versionOption;
    }
    private static Command GetSearchCommand()
    {
        var searchTermArgument = new Argument<string>("searchTerm", "The term to search for.");
        var defaultConfiguration = new SearchConfiguration();

        var logLevelOption = GetLogLevelOption();

        var clearCacheOption = GetClearCacheOption(defaultConfiguration.ClearCache);

        var versionOption = GetVersionOption();

        var searchCommand = new Command("search", "Searches for an OpenAPI description in multiple registries."){
            searchTermArgument,
            logLevelOption,
            clearCacheOption,
            versionOption,
        };
        searchCommand.Handler = new KiotaSearchCommandHandler
        {
            SearchTermArgument = searchTermArgument,
            LogLevelOption = logLevelOption,
            ClearCacheOption = clearCacheOption,
            VersionOption = versionOption,
        };
        return searchCommand;
    }
    private static Option<bool> GetCleanOutputOption(bool defaultValue)
    {
        var cleanOutputOption = new Option<bool>("--clean-output", () => defaultValue, "Removes all files from the output directory before generating the code files.");
        cleanOutputOption.AddAlias("--co");
        return cleanOutputOption;
    }
    internal static Option<string> GetOutputPathOption(string defaultValue)
    {
        var outputOption = new Option<string>("--output", () => defaultValue, "The output directory path for the generated code files.");
        outputOption.AddAlias("-o");
        outputOption.ArgumentHelpName = "path";
        return outputOption;
    }
    internal static Option<List<string>> GetDisableValidationRulesOption()
    {
        var parameterName = "--disable-validation-rules";
        var option = new Option<List<string>>(parameterName, () => new List<string>(), "The OpenAPI description validation rules to disable. Accepts multiple values.");
        option.AddAlias("--dvr");
        var validationRules = new[] {
                                    nameof(DivergentResponseSchema),
                                    nameof(GetWithBody),
                                    nameof(InconsistentTypeFormatPair),
                                    nameof(KnownAndNotSupportedFormats),
                                    nameof(MissingDiscriminator),
                                    nameof(MultipleServerEntries),
                                    nameof(NoContentWithBody),
                                    nameof(NoServerEntry),
                                    nameof(UrlFormEncodedComplex),
                                    nameof(ValidationRuleSetExtensions),
                                    "All"
                                    };
        option.AddValidator(x => ValidateKnownValues(x, parameterName, validationRules));
        option.ArgumentHelpName = string.Join(",", validationRules);
        option.Arity = ArgumentArity.ZeroOrMore;
        return option;
    }
    private static readonly Lazy<bool> isRunningInContainer = new(() =>
    {
        var kiotaInContainerRaw = Environment.GetEnvironmentVariable("KIOTA_CONTAINER");
        return !string.IsNullOrEmpty(kiotaInContainerRaw) && bool.TryParse(kiotaInContainerRaw, out var kiotaInContainer) && kiotaInContainer;
    });
    internal static Option<string> GetDescriptionOption(string defaultValue, bool isRequired = false)
    {
        var descriptionOption = new Option<string>("--openapi", "The path or URI to the OpenAPI description file used to generate the code files.");
        if (isRunningInContainer.Value && !isRequired)
            descriptionOption.SetDefaultValue(defaultValue);
        descriptionOption.AddAlias("-d");
        descriptionOption.ArgumentHelpName = "path";
        descriptionOption.IsRequired = isRequired;
        return descriptionOption;
    }
    private static Option<string> GetManifestOption(string defaultValue)
    {
        var manifestOption = new Option<string>("--manifest", "The path or URI to the API manifest file used to generate the code files. Append #apikey if the target manifest contains multiple API dependencies entries.");
        if (isRunningInContainer.Value)
            manifestOption.SetDefaultValue(defaultValue);
        manifestOption.AddAlias("-a");
        return manifestOption;
    }
    [GeneratedRegex(@"^[a-zA-Z_][\w_-]+", RegexOptions.Singleline, 500)]
    private static partial Regex classNameRegex();
    [GeneratedRegex(@"^[\w][\w\._-]+", RegexOptions.Singleline, 500)]
    private static partial Regex namespaceNameRegex();
    internal static Option<GenerationLanguage> GetLanguageOption()
    {
        var languageOption = new Option<GenerationLanguage>("--language", "The target language for the generated code files.");
        languageOption.AddAlias("-l");
        languageOption.IsRequired = true;
        AddEnumValidator(languageOption, "language");
        return languageOption;
    }
    internal static Option<string> GetNamespaceOption(GenerationConfiguration defaultConfiguration)
    {
        var namespaceOption = new Option<string>("--namespace-name", () => defaultConfiguration.ClientNamespaceName, "The namespace to use for the core client class specified with the --class-name option.");
        namespaceOption.AddAlias("-n");
        namespaceOption.ArgumentHelpName = "name";
        AddStringRegexValidator(namespaceOption, namespaceNameRegex(), "namespace name");
        return namespaceOption;
    }
    internal static Option<bool> GetBackingStoreOption(GenerationConfiguration defaultConfiguration)
    {
        var backingStoreOption = new Option<bool>("--backing-store", () => defaultConfiguration.UsesBackingStore, "Enables backing store for models.");
        backingStoreOption.AddAlias("-b");
        return backingStoreOption;
    }
    internal static Option<bool> GetExcludeBackwardCompatibleOption(GenerationConfiguration defaultConfiguration)
    {
        var excludeBackwardCompatible = new Option<bool>("--exclude-backward-compatible", () => defaultConfiguration.ExcludeBackwardCompatible, "Excludes backward compatible and obsolete assets from the generated result. Should be used for new clients.");
        excludeBackwardCompatible.AddAlias("--ebc");
        return excludeBackwardCompatible;
    }
    internal static Option<bool> GetAdditionalDataOption(GenerationConfiguration defaultConfiguration)
    {
        var additionalDataOption = new Option<bool>("--additional-data", () => defaultConfiguration.IncludeAdditionalData, "Will include the 'AdditionalData' property for models.");
        additionalDataOption.AddAlias("--ad");
        return additionalDataOption;
    }
    internal static Option<List<string>> GetStructuredMimeTypesOption(GenerationConfiguration defaultConfiguration)
    {
        var structuredMimeTypesOption = new Option<List<string>>(
            "--structured-mime-types",
            () => [.. defaultConfiguration.StructuredMimeTypes],
        "The MIME types with optional priorities as defined in RFC9110 Accept header to use for structured data model generation. Accepts multiple values.");
        structuredMimeTypesOption.AddAlias("-m");
        return structuredMimeTypesOption;
    }
    private static Command GetGenerateCommand()
    {
        var defaultConfiguration = new GenerationConfiguration();
        var descriptionOption = GetDescriptionOption(defaultConfiguration.OpenAPIFilePath);
        var manifestOption = GetManifestOption(defaultConfiguration.ApiManifestPath);

        var outputOption = GetOutputPathOption(defaultConfiguration.OutputPath);

        var languageOption = GetLanguageOption();

        var classOption = new Option<string>("--class-name", () => defaultConfiguration.ClientClassName, "The class name to use for the core client class.");
        classOption.AddAlias("-c");
        classOption.ArgumentHelpName = "name";
        AddStringRegexValidator(classOption, classNameRegex(), "class name");

        var namespaceOption = GetNamespaceOption(defaultConfiguration);

        var logLevelOption = GetLogLevelOption();

        var backingStoreOption = GetBackingStoreOption(defaultConfiguration);

        var excludeBackwardCompatible = GetExcludeBackwardCompatibleOption(defaultConfiguration);

        var additionalDataOption = GetAdditionalDataOption(defaultConfiguration);

        var serializerOption = new Option<List<string>>(
            "--serializer",
            () => [.. defaultConfiguration.Serializers],
            "The fully qualified class names for serializers. Accepts multiple values. Use `none` to generate a client without any serializer.");
        serializerOption.AddAlias("-s");
        serializerOption.ArgumentHelpName = "classes";

        var deserializerOption = new Option<List<string>>(
            "--deserializer",
            () => [.. defaultConfiguration.Deserializers],
            "The fully qualified class names for deserializers. Accepts multiple values. Use `none` to generate a client without any deserializer.");
        deserializerOption.AddAlias("--ds");
        deserializerOption.ArgumentHelpName = "classes";

        var cleanOutputOption = GetCleanOutputOption(defaultConfiguration.CleanOutput);

        var structuredMimeTypesOption = GetStructuredMimeTypesOption(defaultConfiguration);

        var (includePatterns, excludePatterns) = GetIncludeAndExcludeOptions(defaultConfiguration.IncludePatterns, defaultConfiguration.ExcludePatterns);

        var dvrOption = GetDisableValidationRulesOption();

        var clearCacheOption = GetClearCacheOption(defaultConfiguration.ClearCache);

        var command = new Command("generate", "Generates a REST HTTP API client from an OpenAPI description file.") {
            descriptionOption,
            manifestOption,
            outputOption,
            languageOption,
            classOption,
            namespaceOption,
            logLevelOption,
            backingStoreOption,
            excludeBackwardCompatible,
            additionalDataOption,
            serializerOption,
            deserializerOption,
            cleanOutputOption,
            structuredMimeTypesOption,
            includePatterns,
            excludePatterns,
            dvrOption,
            clearCacheOption,
        };
        command.Handler = new KiotaGenerateCommandHandler
        {
            DescriptionOption = descriptionOption,
            ManifestOption = manifestOption,
            OutputOption = outputOption,
            LanguageOption = languageOption,
            ClassOption = classOption,
            NamespaceOption = namespaceOption,
            LogLevelOption = logLevelOption,
            BackingStoreOption = backingStoreOption,
            ExcludeBackwardCompatibleOption = excludeBackwardCompatible,
            AdditionalDataOption = additionalDataOption,
            SerializerOption = serializerOption,
            DeserializerOption = deserializerOption,
            CleanOutputOption = cleanOutputOption,
            StructuredMimeTypesOption = structuredMimeTypesOption,
            IncludePatternsOption = includePatterns,
            ExcludePatternsOption = excludePatterns,
            DisabledValidationRulesOption = dvrOption,
            ClearCacheOption = clearCacheOption,
        };
        return command;
    }
    private static Command GetUpdateCommand()
    {
        var defaultConfiguration = new GenerationConfiguration();
        var outputOption = GetOutputPathOption(defaultConfiguration.OutputPath);

        var logLevelOption = GetLogLevelOption();

        var cleanOutputOption = GetCleanOutputOption(defaultConfiguration.CleanOutput);

        var clearCacheOption = GetClearCacheOption(defaultConfiguration.ClearCache);

        var command = new Command("update", "Updates existing clients under the target directory using their lock files.") {
            outputOption,
            logLevelOption,
            cleanOutputOption,
            clearCacheOption,
        };
        command.Handler = new KiotaUpdateCommandHandler
        {
            OutputOption = outputOption,
            LogLevelOption = logLevelOption,
            CleanOutputOption = cleanOutputOption,
            ClearCacheOption = clearCacheOption,
        };
        return command;
    }
    internal static (Option<List<string>>, Option<List<string>>) GetIncludeAndExcludeOptions(HashSet<string> defaultIncludePatterns, HashSet<string> defaultExcludePatterns)
    {
        var includePatterns = new Option<List<string>>(
            "--include-path",
            () => defaultIncludePatterns.ToList(),
            "The paths to include in the generation. Glob patterns accepted. Accepts multiple values. Append #OPERATION to the pattern to specify the operation to include. e.g. users/*/messages#GET");
        includePatterns.AddAlias("-i");

        var excludePatterns = new Option<List<string>>(
            "--exclude-path",
            () => defaultExcludePatterns.ToList(),
            "The paths to exclude from the generation. Glob patterns accepted. Accepts multiple values. Append #OPERATION to the pattern to specify the operation to exclude. e.g. users/*/messages#GET");
        excludePatterns.AddAlias("-e");
        return (includePatterns, excludePatterns);
    }
    internal static Option<LogLevel> GetLogLevelOption()
    {
        var logLevelOption = new Option<LogLevel>("--log-level", () => LogLevel.Warning, "The log level to use when logging messages to the main output.");
        logLevelOption.AddAlias("--ll");
        AddEnumValidator(logLevelOption, "log level");
        return logLevelOption;
    }
    private static Option<bool> GetClearCacheOption(bool defaultValue)
    {
        var clearCacheOption = new Option<bool>("--clear-cache", () => defaultValue, "Clears any cached data for the current command.");
        clearCacheOption.AddAlias("--cc");
        return clearCacheOption;
    }
    private static void AddStringRegexValidator(Option<string> option, Regex validator, string parameterName)
    {
        option.AddValidator(input =>
        {
            var value = input.GetValueForOption(option);
            if (string.IsNullOrEmpty(value) ||
                !validator.IsMatch(value))
                input.ErrorMessage = $"{value} is not a valid {parameterName} for the client, the {parameterName} must conform to {validator}";
        });
    }
    private static void ValidateKnownValues(OptionResult input, string parameterName, IEnumerable<string> knownValues)
    {
        var knownValuesHash = new HashSet<string>(knownValues, StringComparer.OrdinalIgnoreCase);
        if (input.Tokens.Any() && input.Tokens.Select(static x => x.Value).SelectMany(static x => x.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)).FirstOrDefault(x => !knownValuesHash.Contains(x)) is string unknownValue)
        {
            var validOptionsList = knownValues.Aggregate(static (x, y) => x + ", " + y);
            input.ErrorMessage = $"{unknownValue} is not a supported {parameterName}, supported values are {validOptionsList}";
        }
    }
    private static void ValidateEnumValue<T>(OptionResult input, string parameterName) where T : struct, Enum
    {
        if (input.Tokens.Any() && !Enum.TryParse<T>(input.Tokens[0].Value, true, out var _))
        {
            var validOptionsList = Enum.GetValues<T>().Select(static x => x.ToString()).Aggregate(static (x, y) => x + ", " + y);
            input.ErrorMessage = $"{input.Tokens[0].Value} is not a supported generation {parameterName}, supported values are {validOptionsList}";
        }
    }
    private static void AddEnumValidator<T>(Option<T> option, string parameterName) where T : struct, Enum
    {
        option.AddValidator(input =>
        {
            ValidateEnumValue<T>(input, parameterName);
        });
    }
    private static void AddEnumValidator<T>(Option<T?> option, string parameterName) where T : struct, Enum
    {
        option.AddValidator(input =>
        {
            ValidateEnumValue<T>(input, parameterName);
        });
    }
}
