using System.CommandLine;
using kiota.Handlers.Plugin;
using Kiota.Builder;
using Kiota.Builder.Configuration;

namespace kiota;
public static class KiotaPluginCommands
{
    public static Command GetPluginNodeCommand()
    {
        var command = new Command("plugin", "Manages the Kiota generated API plugins");
        command.AddCommand(GetAddCommand());
        command.AddCommand(GetRemoveCommand());
        command.AddCommand(GetEditCommand());
        command.AddCommand(GetGenerateCommand());
        return command;
    }
    internal static Option<string> GetPluginNameOption(bool required = true)
    {
        var clientName = new Option<string>("--plugin-name", "The name of the plugin to manage")
        {
            IsRequired = required,
        };
        clientName.AddAlias("--pn");
        return clientName;
    }
    internal static Option<List<PluginType>> GetPluginTypeOption(bool isRequired = true)
    {
        var typeOption = new Option<List<PluginType>>("--type", "The type of manifest to generate. Accepts multiple values.");
        typeOption.AddAlias("-t");
        if (isRequired)
        {
            typeOption.IsRequired = true;
            typeOption.Arity = ArgumentArity.OneOrMore;
        }
        typeOption.AddCompletions(Enum.GetNames<PluginType>());
        typeOption.AddValidator(x => KiotaHost.ValidateKnownValues(x, "type", Enum.GetNames<PluginType>()));
        return typeOption;
    }
    public static Command GetAddCommand()
    {
        var defaultConfiguration = new GenerationConfiguration();
        var outputOption = KiotaHost.GetOutputPathOption(defaultConfiguration.OutputPath);
        var descriptionOption = KiotaHost.GetDescriptionOption(defaultConfiguration.OpenAPIFilePath, true);
        var (includePatterns, excludePatterns) = KiotaHost.GetIncludeAndExcludeOptions(defaultConfiguration.IncludePatterns, defaultConfiguration.ExcludePatterns);
        var logLevelOption = KiotaHost.GetLogLevelOption();
        var skipGenerationOption = KiotaClientCommands.GetSkipGenerationOption();
        var pluginNameOption = GetPluginNameOption();
        var pluginType = GetPluginTypeOption();
        var command = new Command("add", "Adds a new plugin to the Kiota configuration"){
            descriptionOption,
            includePatterns,
            excludePatterns,
            logLevelOption,
            skipGenerationOption,
            outputOption,
            pluginNameOption,
            pluginType,
            //TODO overlay when we have support for it in OAI.net
        };
        command.Handler = new AddHandler
        {
            ClassOption = pluginNameOption,
            OutputOption = outputOption,
            PluginTypesOption = pluginType,
            DescriptionOption = descriptionOption,
            IncludePatternsOption = includePatterns,
            ExcludePatternsOption = excludePatterns,
            SkipGenerationOption = skipGenerationOption,
            LogLevelOption = logLevelOption,
        };
        return command;
    }
    public static Command GetEditCommand()
    {
        var outputOption = KiotaHost.GetOutputPathOption(string.Empty);
        var descriptionOption = KiotaHost.GetDescriptionOption(string.Empty);
        var (includePatterns, excludePatterns) = KiotaHost.GetIncludeAndExcludeOptions([], []);
        var logLevelOption = KiotaHost.GetLogLevelOption();
        var skipGenerationOption = KiotaClientCommands.GetSkipGenerationOption();
        var pluginNameOption = GetPluginNameOption();
        var pluginTypes = GetPluginTypeOption(false);
        var command = new Command("edit", "Edits a plugin configuration and updates the Kiota configuration"){
            descriptionOption,
            includePatterns,
            excludePatterns,
            logLevelOption,
            skipGenerationOption,
            outputOption,
            pluginNameOption,
            pluginTypes,
            //TODO overlay when we have support for it in OAI.net
        };
        command.Handler = new EditHandler
        {
            ClassOption = pluginNameOption,
            OutputOption = outputOption,
            PluginTypesOption = pluginTypes,
            DescriptionOption = descriptionOption,
            IncludePatternsOption = includePatterns,
            ExcludePatternsOption = excludePatterns,
            SkipGenerationOption = skipGenerationOption,
            LogLevelOption = logLevelOption,
        };
        return command;
    }
    public static Command GetRemoveCommand()
    {
        var pluginNameOption = GetPluginNameOption();
        var cleanOutputOption = KiotaHost.GetCleanOutputOption(false);
        var logLevelOption = KiotaHost.GetLogLevelOption();
        var command = new Command("remove", "Removes a plugin from the Kiota configuration")
        {
            pluginNameOption,
            cleanOutputOption,
            logLevelOption,
        };
        command.Handler = new RemoveHandler
        {
            ClassOption = pluginNameOption,
            CleanOutputOption = cleanOutputOption,
            LogLevelOption = logLevelOption,
        };
        return command;
    }
    public static Command GetGenerateCommand()
    {
        var pluginNameOption = GetPluginNameOption(false);
        var logLevelOption = KiotaHost.GetLogLevelOption();
        var refreshOption = KiotaClientCommands.GetRefreshOption();
        var command = new Command("generate", "Generates one or all plugin from the Kiota configuration")
        {
            pluginNameOption,
            logLevelOption,
            refreshOption,
        };
        command.Handler = new GenerateHandler
        {
            ClassOption = pluginNameOption,
            LogLevelOption = logLevelOption,
            RefreshOption = refreshOption,
        };
        return command;
    }
}
