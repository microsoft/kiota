using System.CommandLine;
using kiota.Handlers.Client;
using Kiota.Builder.Configuration;

namespace kiota;
public static class KiotaClientCommands
{
    public static Command GetClientNodeCommand()
    {
        var command = new Command("client", "Manages the Kiota generated API clients");
        command.AddCommand(GetAddCommand());
        command.AddCommand(GetRemoveCommand());
        command.AddCommand(GetEditCommand());
        command.AddCommand(GetGenerateCommand());
        return command;
    }
    private static Option<bool> GetSkipGenerationOption()
    {
        var skipGeneration = new Option<bool>("--skip-generation", "Skips the generation of the client");
        skipGeneration.AddAlias("--sg");
        return skipGeneration;
    }
    internal static Option<string> GetClientNameOption(bool required = true)
    {
        var clientName = new Option<string>("--client-name", "The name of the client to manage")
        {
            IsRequired = required,
        };
        clientName.AddAlias("--cn");
        return clientName;
    }
    public static Command GetAddCommand()
    {
        var defaultConfiguration = new GenerationConfiguration();
        var languageOption = KiotaHost.GetLanguageOption();
        var outputOption = KiotaHost.GetOutputPathOption(defaultConfiguration.OutputPath);
        var descriptionOption = KiotaHost.GetDescriptionOption(defaultConfiguration.OpenAPIFilePath, true);
        var namespaceOption = KiotaHost.GetNamespaceOption(defaultConfiguration);
        var logLevelOption = KiotaHost.GetLogLevelOption();
        var backingStoreOption = KiotaHost.GetBackingStoreOption(defaultConfiguration);
        var excludeBackwardCompatible = KiotaHost.GetExcludeBackwardCompatibleOption(defaultConfiguration);
        var additionalDataOption = KiotaHost.GetAdditionalDataOption(defaultConfiguration);
        var structuredMimeTypesOption = KiotaHost.GetStructuredMimeTypesOption(defaultConfiguration);
        var (includePatterns, excludePatterns) = KiotaHost.GetIncludeAndExcludeOptions(defaultConfiguration.IncludePatterns, defaultConfiguration.ExcludePatterns);
        var dvrOption = KiotaHost.GetDisableValidationRulesOption();
        var skipGenerationOption = GetSkipGenerationOption();
        var clientNameOption = GetClientNameOption();

        var command = new Command("add", "Adds a new client to the Kiota configuration"){
            descriptionOption,
            outputOption,
            languageOption,
            clientNameOption,
            namespaceOption,
            logLevelOption,
            backingStoreOption,
            excludeBackwardCompatible,
            additionalDataOption,
            structuredMimeTypesOption,
            includePatterns,
            excludePatterns,
            dvrOption,
            skipGenerationOption,
        };
        command.Handler = new AddHandler
        {
            DescriptionOption = descriptionOption,
            OutputOption = outputOption,
            LanguageOption = languageOption,
            ClassOption = clientNameOption,
            NamespaceOption = namespaceOption,
            LogLevelOption = logLevelOption,
            BackingStoreOption = backingStoreOption,
            ExcludeBackwardCompatibleOption = excludeBackwardCompatible,
            AdditionalDataOption = additionalDataOption,
            StructuredMimeTypesOption = structuredMimeTypesOption,
            IncludePatternsOption = includePatterns,
            ExcludePatternsOption = excludePatterns,
            DisabledValidationRulesOption = dvrOption,
            SkipGenerationOption = skipGenerationOption,
        };

        return command;
    }
    public static Command GetRemoveCommand()
    {
        var clientNameOption = GetClientNameOption();
        var cleanOutputOption = KiotaHost.GetCleanOutputOption(false);
        var logLevelOption = KiotaHost.GetLogLevelOption();
        var command = new Command("remove", "Removes a client from the Kiota configuration")
        {
            clientNameOption,
            cleanOutputOption,
            logLevelOption,
        };
        command.Handler = new RemoveHandler
        {
            ClassOption = clientNameOption,
            CleanOutputOption = cleanOutputOption,
            LogLevelOption = logLevelOption,
        };
        return command;
    }
    public static Command GetEditCommand()
    {
        var defaultConfiguration = new GenerationConfiguration();
        var languageOption = KiotaHost.GetOptionalLanguageOption();
        var outputOption = KiotaHost.GetOutputPathOption(defaultConfiguration.OutputPath);
        var descriptionOption = KiotaHost.GetDescriptionOption(defaultConfiguration.OpenAPIFilePath, true);
        var namespaceOption = KiotaHost.GetNamespaceOption(defaultConfiguration);
        var logLevelOption = KiotaHost.GetLogLevelOption();
        var backingStoreOption = KiotaHost.GetBackingStoreOption(defaultConfiguration);
        var excludeBackwardCompatible = KiotaHost.GetExcludeBackwardCompatibleOption(defaultConfiguration);
        var additionalDataOption = KiotaHost.GetAdditionalDataOption(defaultConfiguration);
        var structuredMimeTypesOption = KiotaHost.GetStructuredMimeTypesOption(defaultConfiguration);
        var (includePatterns, excludePatterns) = KiotaHost.GetIncludeAndExcludeOptions(defaultConfiguration.IncludePatterns, defaultConfiguration.ExcludePatterns);
        var dvrOption = KiotaHost.GetDisableValidationRulesOption();
        var skipGenerationOption = GetSkipGenerationOption();
        var clientNameOption = GetClientNameOption();

        var command = new Command("edit", "Edits a client from the Kiota configuration") {
            descriptionOption,
            outputOption,
            languageOption,
            clientNameOption,
            namespaceOption,
            logLevelOption,
            backingStoreOption,
            excludeBackwardCompatible,
            additionalDataOption,
            structuredMimeTypesOption,
            includePatterns,
            excludePatterns,
            dvrOption,
            skipGenerationOption,
        };
        command.Handler = new EditHandler
        {
            DescriptionOption = descriptionOption,
            OutputOption = outputOption,
            LanguageOption = languageOption,
            ClassOption = clientNameOption,
            NamespaceOption = namespaceOption,
            LogLevelOption = logLevelOption,
            BackingStoreOption = backingStoreOption,
            ExcludeBackwardCompatibleOption = excludeBackwardCompatible,
            AdditionalDataOption = additionalDataOption,
            StructuredMimeTypesOption = structuredMimeTypesOption,
            IncludePatternsOption = includePatterns,
            ExcludePatternsOption = excludePatterns,
            DisabledValidationRulesOption = dvrOption,
            SkipGenerationOption = skipGenerationOption,
        };
        return command;
    }
    public static Command GetGenerateCommand()
    {
        var clientNameOption = GetClientNameOption(false);
        var logLevelOption = KiotaHost.GetLogLevelOption();
        var refreshOption = GetRefreshOption();
        var command = new Command("generate", "Generates one or all clients from the Kiota configuration")
        {
            clientNameOption,
            logLevelOption,
            refreshOption,
        };
        command.Handler = new GenerateHandler
        {
            ClassOption = clientNameOption,
            LogLevelOption = logLevelOption,
            RefreshOption = refreshOption,
        };
        return command;
    }
    private static Option<bool> GetRefreshOption()
    {
        var refresh = new Option<bool>("--refresh", "Refreshes the client OpenAPI description before generating the client");
        refresh.AddAlias("--r");
        return refresh;
    }
}
