using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text.RegularExpressions;

using Kiota.Builder;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging;
using kiota.Handlers;

namespace kiota;
public class KiotaHost {
    public RootCommand GetRootCommand() {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(GetGenerateCommand());
        rootCommand.AddCommand(GetSearchCommand());
        rootCommand.AddCommand(GetDownloadCommand());
        rootCommand.AddCommand(GetShowCommand());
        rootCommand.AddCommand(GetInfoCommand());
        rootCommand.AddCommand(GetUpdateCommand());
        rootCommand.AddCommand(GetLoginCommand());
        rootCommand.AddCommand(GetLogoutCommand());
        return rootCommand;
    }
    private static Command GetGitHubLoginCommand() {
        var logLevelOption = GetLogLevelOption();
        var githubLoginCommand = new Command("github", "Logs in to GitHub using a device code flow.")
        {
            logLevelOption,
        };
        githubLoginCommand.Handler = new KiotaGitHubLoginCommandHandler {
            LogLevelOption = logLevelOption,
        };
        return githubLoginCommand;
    }
    private static Command GetGitHubLogoutCommand() {
        var logLevelOption = GetLogLevelOption();
        var githubLogoutCommand = new Command("github", "Logs out of GitHub.") {
            logLevelOption,
        };
        return githubLogoutCommand;
    }
    private static Command GetLoginCommand() {
        var loginCommand = new Command("login", "Logs in to the Kiota registries so search/download/show/generate commands can access private API definitions.");
        loginCommand.AddCommand(GetGitHubLoginCommand());
        return loginCommand;
    }
    private static Command GetLogoutCommand() {
        var loginCommand = new Command("logout", "Logs out of Kiota registries.");
        loginCommand.AddCommand(GetGitHubLogoutCommand());
        return loginCommand;
    }
    private static Command GetInfoCommand() {
        var defaultGenerationConfiguration = new GenerationConfiguration();
        var descriptionOption = GetDescriptionOption(defaultGenerationConfiguration.OpenAPIFilePath);
        descriptionOption.IsRequired = false;
        var versionOption = GetVersionOption();
        var logLevelOption = GetLogLevelOption();
        var clearCacheOption = GetClearCacheOption(defaultGenerationConfiguration.ClearCache);
        var searchTermOption = GetSearchKeyOption();
        var languageOption = new Option<GenerationLanguage?>("--language", "The target language for the dependencies instructions.");
        languageOption.AddAlias("-l");
        AddEnumValidator(languageOption, "language");
        var infoCommand = new Command("info", "Displays information about the languages supported by kiota and dependencies to add in your project.") {
            descriptionOption,
            versionOption,
            logLevelOption,
            clearCacheOption,
            searchTermOption,
            languageOption,
        };
        infoCommand.Handler = new KiotaInfoCommandHandler {
            DescriptionOption = descriptionOption,
            VersionOption = versionOption,
            LogLevelOption = logLevelOption,
            ClearCacheOption = clearCacheOption,
            SearchTermOption = searchTermOption,
            GenerationLanguage = languageOption,
        };
        return infoCommand;
    }
    private static Option<string> GetSearchKeyOption() {
        var option = new Option<string>("--search-key", () => string.Empty, "The API search key to display the description for. Use the search command to get the key.");
        option.AddAlias("-k");
        return option;
    }
    private static Command GetShowCommand() {
        var defaultGenerationConfiguration = new GenerationConfiguration();
        var descriptionOption = GetDescriptionOption(defaultGenerationConfiguration.OpenAPIFilePath);
        descriptionOption.IsRequired = false; // can also use search approach

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
            maxDepthOption,
            includePatterns,
            excludePatterns,
            clearCacheOption,
        };
        displayCommand.Handler = new KiotaShowCommandHandler {
            SearchTermOption = searchTermOption,
            LogLevelOption = logLevelOption,
            VersionOption = versionOption,
            DescriptionOption = descriptionOption,
            MaxDepthOption = maxDepthOption,
            IncludePatternsOption = includePatterns,
            ExcludePatternsOption = excludePatterns,
            ClearCacheOption = clearCacheOption,
        };
        return displayCommand;
    }
    private static Command GetDownloadCommand() {
        var searchTermArgument = new Argument<string>("searchTerm", "The term to search for.");
        var defaultConfiguration = new DownloadConfiguration();
        
        var logLevelOption = GetLogLevelOption();

        var clearCacheOption = GetClearCacheOption(defaultConfiguration.ClearCache);

        var cleanOutputOption = GetCleanOutputOption(defaultConfiguration.CleanOutput);

        var versionOption = GetVersionOption();

        var outputOption = GetOutputPathOption(defaultConfiguration.OutputPath);

        var searchCommand = new Command("download", "Downloads an OpenAPI description from multiple registries."){
            searchTermArgument,
            logLevelOption,
            clearCacheOption,
            versionOption,
            cleanOutputOption,
            outputOption,
        };
        searchCommand.Handler = new KiotaDownloadCommandHandler {
            SearchTermArgument = searchTermArgument,
            LogLevelOption = logLevelOption,
            ClearCacheOption = clearCacheOption,
            VersionOption = versionOption,
            CleanOutputOption = cleanOutputOption,
            OutputPathOption = outputOption,
        };
        return searchCommand;
    }
    private static Option<string> GetVersionOption() {
        var versionOption = new Option<string>("--version", () => string.Empty, "The version of the OpenAPI document to use.");
        versionOption.AddAlias("-v");
        return versionOption;
    }
    private static Command GetSearchCommand() {
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
        searchCommand.Handler = new KiotaSearchCommandHandler {
            SearchTermArgument = searchTermArgument,
            LogLevelOption = logLevelOption,
            ClearCacheOption = clearCacheOption,
            VersionOption = versionOption,
        };
        return searchCommand;
    }
    private static Option<bool> GetCleanOutputOption(bool defaultValue) {
        var cleanOutputOption = new Option<bool>("--clean-output", () => defaultValue, "Removes all files from the output directory before generating the code files.");
        cleanOutputOption.AddAlias("--co");
        return cleanOutputOption;
    }
    private static Option<string> GetOutputPathOption(string defaultValue) {
        var outputOption = new Option<string>("--output", () => defaultValue, "The output directory path for the generated code files.");
        outputOption.AddAlias("-o");
        outputOption.ArgumentHelpName = "path";
        return outputOption;
    }
    private static Option<string> GetDescriptionOption(string defaultValue) {
        var kiotaInContainerRaw = Environment.GetEnvironmentVariable("KIOTA_CONTAINER");
        var runsInContainer = !string.IsNullOrEmpty(kiotaInContainerRaw) && bool.TryParse(kiotaInContainerRaw, out var kiotaInContainer) && kiotaInContainer;
        var descriptionOption = new Option<string>("--openapi", "The path to the OpenAPI description file used to generate the code files.");
        if(runsInContainer)
            descriptionOption.SetDefaultValue(defaultValue);
        else
            descriptionOption.IsRequired = true;
        descriptionOption.AddAlias("-d");
        descriptionOption.ArgumentHelpName = "path";
        return descriptionOption;
    }
    private static Command GetGenerateCommand()
    {
        var defaultConfiguration = new GenerationConfiguration();
        var descriptionOption = GetDescriptionOption(defaultConfiguration.OpenAPIFilePath);

        var outputOption = GetOutputPathOption(defaultConfiguration.OutputPath);
        
        var languageOption = new Option<GenerationLanguage>("--language", "The target language for the generated code files.");
        languageOption.AddAlias("-l");
        languageOption.IsRequired = true;
        AddEnumValidator(languageOption, "language");

        var classOption = new Option<string>("--class-name", () => defaultConfiguration.ClientClassName, "The class name to use for the core client class.");
        classOption.AddAlias("-c");
        classOption.ArgumentHelpName = "name";
        AddStringRegexValidator(classOption, @"^[a-zA-Z_][\w_-]+", "class name");

        var namespaceOption = new Option<string>("--namespace-name", () => defaultConfiguration.ClientNamespaceName, "The namespace to use for the core client class specified with the --class-name option.");
        namespaceOption.AddAlias("-n");
        namespaceOption.ArgumentHelpName = "name";
        AddStringRegexValidator(namespaceOption, @"^[\w][\w\._-]+", "namespace name");

        var logLevelOption = GetLogLevelOption();

        var backingStoreOption = new Option<bool>("--backing-store", () => defaultConfiguration.UsesBackingStore, "Enables backing store for models.");
        backingStoreOption.AddAlias("-b");

        var additionalDataOption = new Option<bool>("--additional-data", () => defaultConfiguration.IncludeAdditionalData, "Will include the 'AdditionalData' property for models.");
        additionalDataOption.AddAlias("--ad");

        var serializerOption = new Option<List<string>>(
            "--serializer", 
            () => defaultConfiguration.Serializers.ToList(),
            "The fully qualified class names for serializers. Accepts multiple values.");
        serializerOption.AddAlias("-s");
        serializerOption.ArgumentHelpName = "classes";

        var deserializerOption = new Option<List<string>>(
            "--deserializer",
            () => defaultConfiguration.Deserializers.ToList(),
            "The fully qualified class names for deserializers. Accepts multiple values.");
        deserializerOption.AddAlias("--ds");
        deserializerOption.ArgumentHelpName = "classes";

        var cleanOutputOption = GetCleanOutputOption(defaultConfiguration.CleanOutput);

        var structuredMimeTypesOption = new Option<List<string>>(
            "--structured-mime-types",
            () => defaultConfiguration.StructuredMimeTypes.ToList(),
        "The MIME types to use for structured data model generation. Accepts multiple values.");
        structuredMimeTypesOption.AddAlias("-m");

        var (includePatterns, excludePatterns) = GetIncludeAndExcludeOptions(defaultConfiguration.IncludePatterns, defaultConfiguration.ExcludePatterns);

        var clearCacheOption = GetClearCacheOption(defaultConfiguration.ClearCache);

        var command = new Command ("generate", "Generates a REST HTTP API client from an OpenAPI description file.") {
            descriptionOption,
            outputOption,
            languageOption,
            classOption,
            namespaceOption,
            logLevelOption,
            backingStoreOption,
            additionalDataOption,
            serializerOption,
            deserializerOption,
            cleanOutputOption,
            structuredMimeTypesOption,
            includePatterns,
            excludePatterns,
            clearCacheOption,
        };
        command.Handler = new KiotaGenerationCommandHandler {
            DescriptionOption = descriptionOption,
            OutputOption = outputOption,
            LanguageOption = languageOption,
            ClassOption = classOption,
            NamespaceOption = namespaceOption,
            LogLevelOption = logLevelOption,
            BackingStoreOption = backingStoreOption,
            AdditionalDataOption = additionalDataOption,
            SerializerOption = serializerOption,
            DeserializerOption = deserializerOption,
            CleanOutputOption = cleanOutputOption,
            StructuredMimeTypesOption = structuredMimeTypesOption,
            IncludePatternsOption = includePatterns,
            ExcludePatternsOption = excludePatterns,
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

        var command = new Command ("update", "Updates existing clients under the target directory using their lock files.") {
            outputOption,
            logLevelOption,
            cleanOutputOption,
            clearCacheOption,
        };
        command.Handler = new KiotaUpdateCommandHandler {
            OutputOption = outputOption,
            LogLevelOption = logLevelOption,
            CleanOutputOption = cleanOutputOption,
            ClearCacheOption = clearCacheOption,
        };
        return command;
    }
    private static (Option<List<string>>, Option<List<string>>) GetIncludeAndExcludeOptions(HashSet<string> defaultIncludePatterns, HashSet<string> defaultExcludePatterns) {
        var includePatterns = new Option<List<string>>(
            "--include-path",
            () => defaultIncludePatterns.ToList(),
            "The paths to include in the generation. Glob patterns accepted. Accepts multiple values.");
        includePatterns.AddAlias("-i");

        var excludePatterns = new Option<List<string>>(
            "--exclude-path",
            () => defaultExcludePatterns.ToList(),
            "The paths to exclude from the generation. Glob patterns accepted. Accepts multiple values.");
        excludePatterns.AddAlias("-e");
        return (includePatterns, excludePatterns);
    }
    private static Option<LogLevel> GetLogLevelOption() {
        var logLevelOption = new Option<LogLevel>("--log-level", () => LogLevel.Warning, "The log level to use when logging messages to the main output.");
        logLevelOption.AddAlias("--ll");
        AddEnumValidator(logLevelOption, "log level");
        return logLevelOption;
    }
    private static Option<bool> GetClearCacheOption(bool defaultValue) {
        var clearCacheOption = new Option<bool>("--clear-cache", () => defaultValue, "Clears any cached data for the current command.");
        clearCacheOption.AddAlias("--cc");
        return clearCacheOption;
    }
    private static void AddStringRegexValidator(Option<string> option, string pattern, string parameterName) {
        var validator = new Regex(pattern);
        option.AddValidator(input => {
            var value = input.GetValueForOption(option);
            if(string.IsNullOrEmpty(value) ||
                !validator.IsMatch(value))
                    input.ErrorMessage = $"{value} is not a valid {parameterName} for the client, the {parameterName} must conform to {pattern}";
        });
    }
    private static void ValidateEnumValue<T>(OptionResult input, string parameterName) where T: struct, Enum {
        if(input.Tokens.Any() && !Enum.TryParse<T>(input.Tokens[0].Value, true, out var _)) {
            var validOptionsList = Enum.GetValues<T>().Select(static x => x.ToString()).Aggregate(static (x, y) => x + ", " + y);
            input.ErrorMessage = $"{input.Tokens[0].Value} is not a supported generation {parameterName}, supported values are {validOptionsList}";
        }
    }
    private static void AddEnumValidator<T>(Option<T> option, string parameterName) where T: struct, Enum {
        option.AddValidator(input => {
            ValidateEnumValue<T>(input, parameterName);
        });
    }
    private static void AddEnumValidator<T>(Option<T?> option, string parameterName) where T: struct, Enum {
        option.AddValidator(input => {
            ValidateEnumValue<T>(input, parameterName);
        });
    }
}
