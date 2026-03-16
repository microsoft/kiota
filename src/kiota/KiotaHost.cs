using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.RegularExpressions;
using kiota.Handlers;
using kiota.Rpc;
using Kiota.Builder;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Validation;
using Microsoft.Extensions.Logging;

namespace kiota;

public static partial class KiotaHost
{
    internal const string KiotaPreviewEnvironmentVariable = "KIOTA_CONFIG_PREVIEW";
    internal static readonly Lazy<bool> IsConfigPreviewEnabled = new(() => bool.TryParse(Environment.GetEnvironmentVariable(KiotaPreviewEnvironmentVariable), out var isPreviewEnabled) && isPreviewEnabled);
    public static RootCommand GetRootCommand(IServiceProvider? serviceProvider)
    {
        var rootCommand = new RootCommand();
        if (!IsConfigPreviewEnabled.Value)
            rootCommand.Add(GetGenerateCommand(serviceProvider));
        rootCommand.Add(GetSearchCommand(serviceProvider));
        rootCommand.Add(GetDownloadCommand(serviceProvider));
        rootCommand.Add(GetShowCommand(serviceProvider));
        rootCommand.Add(GetInfoCommand(serviceProvider));
        if (!IsConfigPreviewEnabled.Value)
            rootCommand.Add(GetUpdateCommand(serviceProvider));
        rootCommand.Add(GetLoginCommand(serviceProvider));
        rootCommand.Add(GetLogoutCommand(serviceProvider));
        rootCommand.Add(GetRpcCommand(serviceProvider));
        if (IsConfigPreviewEnabled.Value)
        {
            rootCommand.Add(KiotaWorkspaceCommands.GetWorkspaceNodeCommand(serviceProvider));
            rootCommand.Add(KiotaClientCommands.GetClientNodeCommand(serviceProvider));
            rootCommand.Add(KiotaPluginCommands.GetPluginNodeCommand(serviceProvider));
        }
        return rootCommand;
    }
    private static Command GetGitHubLoginCommand(IServiceProvider? serviceProvider)
    {
        var githubLoginCommand = new Command("github", "Logs in to GitHub.");
        githubLoginCommand.Add(GetGitHubDeviceLoginCommand(serviceProvider));
        githubLoginCommand.Add(GetGitHubPatLoginCommand(serviceProvider));
        return githubLoginCommand;
    }
    private static Command GetGitHubDeviceLoginCommand(IServiceProvider? serviceProvider)
    {
        var logLevelOption = GetLogLevelOption();
        var deviceLoginCommand = new Command("device", "Logs in to GitHub using a device code flow.")
        {
            logLevelOption,
        };
        deviceLoginCommand.Action = new KiotaGitHubDeviceLoginCommandHandler
        {
            LogLevelOption = logLevelOption,
            ServiceProvider = serviceProvider,
        };
        return deviceLoginCommand;
    }
    private static Command GetGitHubPatLoginCommand(IServiceProvider? serviceProvider)
    {
        var logLevelOption = GetLogLevelOption();
        var patOption = new Option<string>("--pat")
        {
            Description = "The personal access token to use to authenticate to GitHub.",
            Required = true
        };
        var deviceLoginCommand = new Command("pat", "Logs in to GitHub using a Personal Access Token.")
        {
            logLevelOption,
            patOption,
        };
        deviceLoginCommand.Action = new KiotaGitHubPatLoginCommandHandler
        {
            LogLevelOption = logLevelOption,
            PatOption = patOption,
            ServiceProvider = serviceProvider,
        };
        return deviceLoginCommand;
    }
    private static Command GetGitHubLogoutCommand(IServiceProvider? serviceProvider)
    {
        var logLevelOption = GetLogLevelOption();
        var githubLogoutCommand = new Command("github", "Logs out of GitHub.") {
            logLevelOption,
        };
        githubLogoutCommand.Action = new KiotaGitHubLogoutCommandHandler
        {
            LogLevelOption = logLevelOption,
            ServiceProvider = serviceProvider,
        };
        return githubLogoutCommand;
    }
    private static Command GetLoginCommand(IServiceProvider? serviceProvider)
    {
        var loginCommand = new Command("login", "Logs in to the Kiota registries so search/download/show/generate commands can access private API definitions.");
        loginCommand.Add(GetGitHubLoginCommand(serviceProvider));
        return loginCommand;
    }
    private static Command GetLogoutCommand(IServiceProvider? serviceProvider)
    {
        var loginCommand = new Command("logout", "Logs out of Kiota registries.");
        loginCommand.Add(GetGitHubLogoutCommand(serviceProvider));
        return loginCommand;
    }
    private static Command GetInfoCommand(IServiceProvider? serviceProvider)
    {
        var defaultGenerationConfiguration = new GenerationConfiguration();
        var descriptionOption = GetDescriptionOption(defaultGenerationConfiguration.OpenAPIFilePath);
        var manifestOption = GetManifestOption(defaultGenerationConfiguration.ApiManifestPath);
        var versionOption = GetVersionOption();
        var logLevelOption = GetLogLevelOption();
        var clearCacheOption = GetClearCacheOption(defaultGenerationConfiguration.ClearCache);
        var searchTermOption = GetSearchKeyOption();
        var languageOption = new Option<GenerationLanguage?>("--language")
        {
            Description = "The target language for the dependencies instructions.",
        };
        var dependencyTypesOption = GetDependencyTypesOption();
        var jsonOption = new Option<bool>("--json")
        {
            Description = "Generate a plain and machine-parsable json output.",
        };
        languageOption.Aliases.Add("-l");
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
            dependencyTypesOption,
        };
        infoCommand.Action = new KiotaInfoCommandHandler
        {
            DescriptionOption = descriptionOption,
            ManifestOption = manifestOption,
            VersionOption = versionOption,
            LogLevelOption = logLevelOption,
            ClearCacheOption = clearCacheOption,
            SearchTermOption = searchTermOption,
            GenerationLanguage = languageOption,
            JsonOption = jsonOption,
            DependencyTypesOption = dependencyTypesOption,
            ServiceProvider = serviceProvider,
        };
        return infoCommand;
    }
    private static Option<DependencyType[]> GetDependencyTypesOption()
    {
        var dependencyTypesOption = new Option<DependencyType[]>("--dependency-type")
        {
            Description = "The type of dependency to display instructions for.",
            Required = false,
            Arity = ArgumentArity.ZeroOrMore,
        };
        dependencyTypesOption.Aliases.Add("--dt");
        dependencyTypesOption.DefaultValueFactory = _ => Array.Empty<DependencyType>();
        dependencyTypesOption.CompletionSources.Add(Enum.GetNames<DependencyType>());
        return dependencyTypesOption;
    }
    private static Option<string> GetSearchKeyOption()
    {
        var option = new Option<string>("--search-key")
        {
            DefaultValueFactory = _ => string.Empty,
            Description = "The API search key to display the description for. Use the search command to get the key.",
        };
        option.Aliases.Add("-k");
        return option;
    }
    private static Command GetShowCommand(IServiceProvider? serviceProvider)
    {
        var defaultGenerationConfiguration = new GenerationConfiguration();
        var descriptionOption = GetDescriptionOption(defaultGenerationConfiguration.OpenAPIFilePath);
        var manifestOption = GetManifestOption(defaultGenerationConfiguration.ApiManifestPath);

        var versionOption = GetVersionOption();
        var logLevelOption = GetLogLevelOption();
        var (includePatterns, excludePatterns) = GetIncludeAndExcludeOptions(defaultGenerationConfiguration.IncludePatterns, defaultGenerationConfiguration.ExcludePatterns);
        var clearCacheOption = GetClearCacheOption(defaultGenerationConfiguration.ClearCache);
        var searchTermOption = GetSearchKeyOption();
        var maxDepthOption = new Option<uint>("--max-depth")
        {
            DefaultValueFactory = _ => 5u,
            Description = "The maximum depth of the tree to display",
        };
        maxDepthOption.Aliases.Add("--m-d");
        var disableSSLValidationOption = GetDisableSSLValidationOption(defaultGenerationConfiguration.DisableSSLValidation);
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
            disableSSLValidationOption,
        };
        displayCommand.Action = new KiotaShowCommandHandler
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
            DisableSSLValidationOption = disableSSLValidationOption,
            ServiceProvider = serviceProvider,
        };
        return displayCommand;
    }
    internal static Command GetRpcCommand(IServiceProvider? serviceProvider)
    {
        var modeOption = new Option<RpcMode>("--mode")
        {
            Description = "Whether the RPC server should use stdin/stdout or a named pipe.",
        };
        modeOption.Aliases.Add("-m");
        modeOption.DefaultValueFactory = _ => RpcMode.Stdio;
        var pipeNameOption = new Option<string>("--pipe-name")
        {
            Description = "The name of the named pipe to use for the RPC server.",
        };
        pipeNameOption.Aliases.Add("-p");
        pipeNameOption.DefaultValueFactory = _ => "KiotaJsonRpc";
        var commandHandler = new KiotaRpcCommandHandler
        {
            ModeOption = modeOption,
            PipeNameOption = pipeNameOption,
            ServiceProvider = serviceProvider,
        };
        var command = new Command("rpc", "WARNING EXPERIMENTAL: Starts a kiota as a JSON-RPC server.") {
            modeOption,
            pipeNameOption,
        };
        command.Action = commandHandler;
        return command;
    }
    private static Command GetDownloadCommand(IServiceProvider? serviceProvider)
    {
        var keyArgument = new Argument<string>("key")
        {
            Description = "The search result key to download the description for. Use the search command to get the key.",
        };
        var defaultConfiguration = new DownloadConfiguration();

        var logLevelOption = GetLogLevelOption();

        var clearCacheOption = GetClearCacheOption(defaultConfiguration.ClearCache);

        var cleanOutputOption = GetCleanOutputOption(defaultConfiguration.CleanOutput);

        var versionOption = GetVersionOption();

        var outputOption = GetOutputPathOption(defaultConfiguration.OutputPath);

        var disableSSLValidationOption = GetDisableSSLValidationOption(defaultConfiguration.DisableSSLValidation);

        var searchCommand = new Command("download", "Downloads an OpenAPI description from multiple registries."){
            keyArgument,
            logLevelOption,
            clearCacheOption,
            versionOption,
            cleanOutputOption,
            outputOption,
            disableSSLValidationOption,
        };
        searchCommand.Action = new KiotaDownloadCommandHandler
        {
            SearchTermArgument = keyArgument,
            LogLevelOption = logLevelOption,
            ClearCacheOption = clearCacheOption,
            VersionOption = versionOption,
            CleanOutputOption = cleanOutputOption,
            OutputPathOption = outputOption,
            DisableSSLValidationOption = disableSSLValidationOption,
            ServiceProvider = serviceProvider,
        };
        return searchCommand;
    }
    private static Option<string> GetVersionOption()
    {
        var versionOption = new Option<string>("--version")
        {
            DefaultValueFactory = _ => string.Empty,
            Description = "The version of the OpenAPI document to use.",
        };
        versionOption.Aliases.Add("-v");
        return versionOption;
    }
    private static Command GetSearchCommand(IServiceProvider? serviceProvider)
    {
        var searchTermArgument = new Argument<string>("searchTerm")
        {
            Description = "The term to search for.",
        };
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
        searchCommand.Action = new KiotaSearchCommandHandler
        {
            SearchTermArgument = searchTermArgument,
            LogLevelOption = logLevelOption,
            ClearCacheOption = clearCacheOption,
            VersionOption = versionOption,
            ServiceProvider = serviceProvider,
        };
        return searchCommand;
    }
    internal static Option<bool> GetCleanOutputOption(bool defaultValue)
    {
        var cleanOutputOption = new Option<bool>("--clean-output")
        {
            DefaultValueFactory = _ => defaultValue,
            Description = "Removes all files from the output directory before generating the code files.",
        };
        cleanOutputOption.Aliases.Add("--co");
        return cleanOutputOption;
    }
    internal static Option<string> GetOutputPathOption(string defaultValue)
    {
        var outputOption = new Option<string>("--output")
        {
            DefaultValueFactory = _ => defaultValue,
            Description = "The output directory path for the generated code files.",
        };
        outputOption.Aliases.Add("-o");
        outputOption.HelpName = "path";
        return outputOption;
    }
    internal static Option<List<string>> GetDisableValidationRulesOption()
    {
        var parameterName = "--disable-validation-rules";
        var option = new Option<List<string>>(parameterName)
        {
            DefaultValueFactory = _ => [],
            Description = "The OpenAPI description validation rules to disable. Accepts multiple values.",
        };
        option.Aliases.Add("--dvr");
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
        option.Validators.Add(x => ValidateKnownValues(x, parameterName, validationRules));
        option.HelpName = string.Join(",", validationRules);
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
        var descriptionOption = new Option<string>("--openapi")
        {
            Description = "The path or URI to the OpenAPI description file used to generate the code files.",
        };
        if (isRunningInContainer.Value && !isRequired)
            descriptionOption.DefaultValueFactory = _ => defaultValue;
        descriptionOption.Aliases.Add("-d");
        descriptionOption.HelpName = "path";
        descriptionOption.Required = isRequired;
        return descriptionOption;
    }
    private static Option<string> GetManifestOption(string defaultValue)
    {
        var manifestOption = new Option<string>("--manifest")
        {
            Description = "The path or URI to the API manifest file used to generate the code files. Append #apikey if the target manifest contains multiple API dependencies entries.",
        };
        if (isRunningInContainer.Value)
            manifestOption.DefaultValueFactory = _ => defaultValue;
        manifestOption.Aliases.Add("-a");
        return manifestOption;
    }
    [GeneratedRegex(@"^[a-zA-Z_][\w]*", RegexOptions.Singleline, 500)]
    private static partial Regex classNameRegex();
    [GeneratedRegex(@"^[\w][\w\._-]+", RegexOptions.Singleline, 500)]
    private static partial Regex namespaceNameRegex();
    internal static Option<GenerationLanguage> GetLanguageOption()
    {
        var languageOption = new Option<GenerationLanguage>("--language")
        {
            Description = "The target language for the generated code files.",
        };
        languageOption.Aliases.Add("-l");
        languageOption.Required = true;
        AddEnumValidator(languageOption, "language");
        return languageOption;
    }
    internal static Option<GenerationLanguage?> GetOptionalLanguageOption()
    {
        var languageOption = new Option<GenerationLanguage?>("--language")
        {
            Description = "The target language for the generated code files.",
        };
        languageOption.Aliases.Add("-l");
        AddEnumValidator(languageOption, "language");
        return languageOption;
    }
    internal static Option<AccessModifier> GetTypeAccessModifierOption()
    {
        var accessOption = new Option<AccessModifier>("--type-access-modifier")
        {
            Description = "The type access modifier to use for the client types.",
        };
        accessOption.Aliases.Add("--tam");
        accessOption.DefaultValueFactory = _ => AccessModifier.Public;
        AddEnumValidator(accessOption, "type-access-modifier");
        return accessOption;
    }
    internal static Option<AccessModifier?> GetOptionalTypeAccessModifierOption()
    {
        var accessOption = new Option<AccessModifier?>("--type-access-modifier")
        {
            Description = "The type access modifier to use for the client types.",
        };
        accessOption.Aliases.Add("--tam");
        AddEnumValidator(accessOption, "type-access-modifier");
        return accessOption;
    }
    internal static Option<string> GetNamespaceOption(string defaultNamespaceName)
    {
        var namespaceOption = new Option<string>("--namespace-name")
        {
            DefaultValueFactory = _ => defaultNamespaceName,
            Description = "The namespace to use for the core client class specified with the --class-name option.",
        };
        namespaceOption.Aliases.Add("-n");
        namespaceOption.HelpName = "name";
        AddStringRegexValidator(namespaceOption, namespaceNameRegex(), "namespace name", string.IsNullOrEmpty(defaultNamespaceName));
        return namespaceOption;
    }
    internal static Option<bool> GetBackingStoreOption(bool defaultValue = false)
    {
        var backingStoreOption = new Option<bool>("--backing-store")
        {
            DefaultValueFactory = _ => defaultValue,
            Description = "Enables backing store for models.",
        };
        backingStoreOption.Aliases.Add("-b");
        return backingStoreOption;
    }
    internal static Option<bool?> GetOptionalBackingStoreOption()
    {
        var backingStoreOption = new Option<bool?>("--backing-store")
        {
            Description = "Enables backing store for models.",
        };
        backingStoreOption.Aliases.Add("-b");
        return backingStoreOption;
    }
    internal static Option<bool> GetExcludeBackwardCompatibleOption(bool defaultValue = false)
    {
        var excludeBackwardCompatible = new Option<bool>("--exclude-backward-compatible")
        {
            DefaultValueFactory = _ => defaultValue,
            Description = "Excludes backward compatible and obsolete assets from the generated result. Should be used for new clients.",
        };
        excludeBackwardCompatible.Aliases.Add("--ebc");
        return excludeBackwardCompatible;
    }
    internal static Option<bool?> GetOptionalExcludeBackwardCompatibleOption()
    {
        var excludeBackwardCompatible = new Option<bool?>("--exclude-backward-compatible")
        {
            Description = "Excludes backward compatible and obsolete assets from the generated result. Should be used for new clients.",
        };
        excludeBackwardCompatible.Aliases.Add("--ebc");
        return excludeBackwardCompatible;
    }
    internal static Option<bool> GetAdditionalDataOption(bool defaultValue = true)
    {
        var additionalDataOption = new Option<bool>("--additional-data")
        {
            DefaultValueFactory = _ => defaultValue,
            Description = "Will include the 'AdditionalData' property for models.",
        };
        additionalDataOption.Aliases.Add("--ad");
        return additionalDataOption;
    }
    internal static Option<bool?> GetOptionalAdditionalDataOption()
    {
        var additionalDataOption = new Option<bool?>("--additional-data")
        {
            Description = "Will include the 'AdditionalData' property for models.",
        };
        additionalDataOption.Aliases.Add("--ad");
        return additionalDataOption;
    }
    internal static Option<List<string>> GetStructuredMimeTypesOption(List<string> defaultValue)
    {
        var structuredMimeTypesOption = new Option<List<string>>("--structured-mime-types")
        {
            DefaultValueFactory = _ => defaultValue,
            Description = "The MIME types with optional priorities as defined in RFC9110 Accept header to use for structured data model generation. Accepts multiple values.",
        };
        structuredMimeTypesOption.Aliases.Add("-m");
        return structuredMimeTypesOption;
    }
    private static Command GetGenerateCommand(IServiceProvider? serviceProvider)
    {
        var defaultConfiguration = new GenerationConfiguration();
        var descriptionOption = GetDescriptionOption(defaultConfiguration.OpenAPIFilePath);
        var manifestOption = GetManifestOption(defaultConfiguration.ApiManifestPath);

        var outputOption = GetOutputPathOption(defaultConfiguration.OutputPath);

        var languageOption = GetLanguageOption();

        var classOption = new Option<string>("--class-name")
        {
            DefaultValueFactory = _ => defaultConfiguration.ClientClassName,
            Description = "The class name to use for the core client class.",
        };
        classOption.Aliases.Add("-c");
        classOption.HelpName = "name";
        AddStringRegexValidator(classOption, classNameRegex(), "class name");

        var typeAccessModifierOption = GetTypeAccessModifierOption();

        var namespaceOption = GetNamespaceOption(defaultConfiguration.ClientNamespaceName);

        var logLevelOption = GetLogLevelOption();

        var backingStoreOption = GetBackingStoreOption(defaultConfiguration.UsesBackingStore);

        var excludeBackwardCompatible = GetExcludeBackwardCompatibleOption(defaultConfiguration.ExcludeBackwardCompatible);

        var additionalDataOption = GetAdditionalDataOption(defaultConfiguration.IncludeAdditionalData);

        var serializerOption = new Option<List<string>>("--serializer")
        {
            DefaultValueFactory = _ => [.. defaultConfiguration.Serializers],
            Description = "The fully qualified class names for serializers. Accepts multiple values. Use `none` to generate a client without any serializer.",
        };
        serializerOption.Aliases.Add("-s");
        serializerOption.HelpName = "classes";

        var deserializerOption = new Option<List<string>>("--deserializer")
        {
            DefaultValueFactory = _ => [.. defaultConfiguration.Deserializers],
            Description = "The fully qualified class names for deserializers. Accepts multiple values. Use `none` to generate a client without any deserializer.",
        };
        deserializerOption.Aliases.Add("--ds");
        deserializerOption.HelpName = "classes";

        var cleanOutputOption = GetCleanOutputOption(defaultConfiguration.CleanOutput);

        var structuredMimeTypesOption = GetStructuredMimeTypesOption([.. defaultConfiguration.StructuredMimeTypes]);

        var (includePatterns, excludePatterns) = GetIncludeAndExcludeOptions(defaultConfiguration.IncludePatterns, defaultConfiguration.ExcludePatterns);

        var dvrOption = GetDisableValidationRulesOption();

        var clearCacheOption = GetClearCacheOption(defaultConfiguration.ClearCache);

        var disableSSLValidationOption = GetDisableSSLValidationOption(defaultConfiguration.DisableSSLValidation);

        var command = new Command("generate", "Generates a REST HTTP API client from an OpenAPI description file.") {
            descriptionOption,
            manifestOption,
            outputOption,
            languageOption,
            classOption,
            typeAccessModifierOption,
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
            disableSSLValidationOption,
        };
        command.Action = new KiotaGenerateCommandHandler
        {
            DescriptionOption = descriptionOption,
            ManifestOption = manifestOption,
            OutputOption = outputOption,
            LanguageOption = languageOption,
            ClassOption = classOption,
            TypeAccessModifierOption = typeAccessModifierOption,
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
            DisableSSLValidationOption = disableSSLValidationOption,
            ServiceProvider = serviceProvider,
        };
        return command;
    }
    private static Command GetUpdateCommand(IServiceProvider? serviceProvider)
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
        command.Action = new KiotaUpdateCommandHandler
        {
            OutputOption = outputOption,
            LogLevelOption = logLevelOption,
            CleanOutputOption = cleanOutputOption,
            ClearCacheOption = clearCacheOption,
            ServiceProvider = serviceProvider,
        };
        return command;
    }
    internal static (Option<List<string>>, Option<List<string>>) GetIncludeAndExcludeOptions(HashSet<string> defaultIncludePatterns, HashSet<string> defaultExcludePatterns)
    {
        var includePatterns = new Option<List<string>>("--include-path")
        {
            DefaultValueFactory = _ => defaultIncludePatterns.ToList(),
            Description = "The paths to include in the generation. Glob patterns accepted. Accepts multiple values. Append #OPERATION to the pattern to specify the operation to include. e.g. users/*/messages#GET",
        };
        includePatterns.Aliases.Add("-i");

        var excludePatterns = new Option<List<string>>("--exclude-path")
        {
            DefaultValueFactory = _ => defaultExcludePatterns.ToList(),
            Description = "The paths to exclude from the generation. Glob patterns accepted. Accepts multiple values. Append #OPERATION to the pattern to specify the operation to exclude. e.g. users/*/messages#GET",
        };
        excludePatterns.Aliases.Add("-e");
        return (includePatterns, excludePatterns);
    }
    internal static Option<LogLevel> GetLogLevelOption()
    {
#if DEBUG
        static LogLevel DefaultLogLevel() => LogLevel.Debug;
#else
        static LogLevel DefaultLogLevel() => LogLevel.Warning;
#endif
        var logLevelOption = new Option<LogLevel>("--log-level")
        {
            DefaultValueFactory = _ => DefaultLogLevel(),
            Description = "The log level to use when logging messages to the main output.",
        };
        logLevelOption.Aliases.Add("--ll");
        AddEnumValidator(logLevelOption, "log level");
        return logLevelOption;
    }
    private static Option<bool> GetClearCacheOption(bool defaultValue)
    {
        var clearCacheOption = new Option<bool>("--clear-cache")
        {
            DefaultValueFactory = _ => defaultValue,
            Description = "Clears any cached data for the current command.",
        };
        clearCacheOption.Aliases.Add("--cc");
        return clearCacheOption;
    }

    private static Option<bool> GetDisableSSLValidationOption(bool defaultValue)
    {
        var disableSSLValidationOption = new Option<bool>("--disable-ssl-validation")
        {
            DefaultValueFactory = _ => defaultValue,
            Description = "Disables SSL certificate validation.",
        };
        disableSSLValidationOption.Aliases.Add("--dsv");
        return disableSSLValidationOption;
    }

    private static void AddStringRegexValidator(Option<string> option, Regex validator, string parameterName, bool allowEmpty = false)
    {
        option.Validators.Add(input =>
        {
            var value = input.GetValue(option);
            if (string.IsNullOrEmpty(value) && allowEmpty) return;
            if (string.IsNullOrEmpty(value) ||
                !validator.IsMatch(value))
                input.AddError($"{value} is not a valid {parameterName} for the client, the {parameterName} must conform to {validator}");
        });
    }
    internal static void ValidateAllOrNoneOptions(CommandResult commandResult, params Option[] options)
    {
        var optionResults = options.Select(option => commandResult.GetResult(option));
        var optionsWithValue = optionResults.Where(result => result?.Tokens.Any() ?? false).ToList();

        // If not all options are set and at least one is set, it's an error
        if (optionsWithValue.Count > 0 && optionsWithValue.Count < options.Length)
        {
            var optionNames = options.Select(option => option.Aliases.FirstOrDefault() ?? "unknown option").ToArray();
            commandResult.AddError($"Either all of {string.Join(", ", optionNames)} must be provided or none.");
        }
    }
    internal static void ValidateKnownValues(OptionResult input, string parameterName, IEnumerable<string> knownValues)
    {
        var knownValuesHash = new HashSet<string>(knownValues, StringComparer.OrdinalIgnoreCase);
        if (input.Tokens.Any() && input.Tokens.Select(static x => x.Value).SelectMany(static x => x.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)).FirstOrDefault(x => !knownValuesHash.Contains(x)) is string unknownValue)
        {
            var validOptionsList = knownValues.Aggregate(static (x, y) => x + ", " + y);
            input.AddError($"{unknownValue} is not a supported {parameterName}, supported values are {validOptionsList}");
        }
    }
    private static void AddEnumValidator<T>(Option<T> option, string parameterName) where T : struct, Enum
    {
        option.Validators.Add(input =>
        {
            ValidateKnownValues(input, parameterName, Enum.GetValues<T>().Select(static x => x.ToString()));
        });
    }
    private static void AddEnumValidator<T>(Option<T?> option, string parameterName) where T : struct, Enum
    {
        option.Validators.Add(input =>
        {
            ValidateKnownValues(input, parameterName, Enum.GetValues<T>().Select(static x => x.ToString()));
        });
    }
}
