using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text.RegularExpressions;

using Kiota.Builder;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging;

namespace kiota;
public class KiotaHost {
    public RootCommand GetRootCommand() {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(GetGenerateCommand());
        rootCommand.AddCommand(GetSearchCommand());
        rootCommand.AddCommand(GetDownloadCommand());
        rootCommand.AddCommand(GetDisplayCommand());
        return rootCommand;
    }
    private static Command GetDisplayCommand() {
        var defaultSearchConfiguration = new SearchConfiguration();
        var defaultGenerationConfiguration = new GenerationConfiguration();
        var descriptionOption = GetDescriptionOption(defaultGenerationConfiguration.OpenAPIFilePath);

        var versionOption = GetVersionOption();
        var logLevelOption = GetLogLevelOption();
        var searchTermOption = new Option<string>("--search-term", () => string.Empty, "The term to search for.");
        var maxDepthOption = new Option<uint>("--max-depth", () => 5, "The maximum depth of the tree to display");
        var displayCommand = new Command("display", "Displays the API paths in a given description."){
            searchTermOption,
            logLevelOption,
            versionOption,
            descriptionOption,
            maxDepthOption,
        };
        displayCommand.Handler = new KiotaDisplayCommandHandler {
            SearchTermOption = searchTermOption,
            LogLevelOption = logLevelOption,
            VersionOption = versionOption,
            DescriptionOption = descriptionOption,
            MaxDepthOption = maxDepthOption,
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

        var includePatterns = new Option<List<string>>(
            "--include-path",
            () => defaultConfiguration.IncludePatterns.ToList(),
            "The paths to include in the generation. Glob patterns accepted. Accepts multiple values.");
        includePatterns.AddAlias("-i");

        var excludePatterns = new Option<List<string>>(
            "--exclude-path",
            () => defaultConfiguration.ExcludePatterns.ToList(),
            "The paths to exclude from the generation. Glob patterns accepted. Accepts multiple values.");
        excludePatterns.AddAlias("-e");

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
    private static void AddEnumValidator<T>(Option<T> option, string parameterName) where T: struct, Enum {
        option.AddValidator(input => {
            if(input.Tokens.Any() &&
                !Enum.TryParse<T>(input.Tokens[0].Value, true, out var _)) {
                    var validOptionsList = Enum.GetValues<T>().Select(x => x.ToString()).Aggregate((x, y) => x + ", " + y);
                    input.ErrorMessage = $"{input.Tokens[0].Value} is not a supported generation {parameterName}, supported values are {validOptionsList}";
                }
        });
    }
}
