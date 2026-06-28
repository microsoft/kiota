using System.CommandLine;
using kiota.Handlers.Plugin;
using Kiota.Builder;
using Kiota.Builder.Configuration;
using Microsoft.OpenApi;

namespace kiota;

public static class KiotaPluginCommands
{
    public static Command GetPluginNodeCommand(IServiceProvider serviceProvider)
    {
        var command = new Command("plugin", "Manages the Kiota generated API plugins");
        command.Add(GetAddCommand(serviceProvider));
        command.Add(GetRemoveCommand(serviceProvider));
        command.Add(GetEditCommand(serviceProvider));
        command.Add(GetGenerateCommand(serviceProvider));
        return command;
    }
    internal static Option<string> GetPluginNameOption(bool required = true)
    {
        var clientName = new Option<string>("--plugin-name")
        {
            Description = "The name of the plugin to manage",
            Required = required,
        };
        clientName.Aliases.Add("--pn");
        return clientName;
    }
    internal static Option<List<PluginType>> GetPluginTypeOption(bool isRequired = true)
    {
        var typeOption = new Option<List<PluginType>>("--type")
        {
            Description = "The type of manifest to generate. Accepts multiple values.",
        };
        typeOption.Aliases.Add("-t");
        if (isRequired)
        {
            typeOption.Required = true;
            typeOption.Arity = ArgumentArity.OneOrMore;
        }
        typeOption.CompletionSources.Add(Enum.GetNames<PluginType>());
        typeOption.Validators.Add(x => KiotaHost.ValidateKnownValues(x, "type", Enum.GetNames<PluginType>()));
        return typeOption;
    }

    internal static Option<SecuritySchemeType> GetPluginAuthenticationTypeOption(bool isRequired = false)
    {
        var authTypeOption = new Option<SecuritySchemeType>("--authentication-type")
        {
            Description = "The authentication type for the plugin. Should be a valid OpenAPI security scheme.",
        };
        authTypeOption.Aliases.Add("--at");
        authTypeOption.Required = isRequired;
        authTypeOption.Arity = ArgumentArity.ZeroOrOne;
        authTypeOption.Validators.Add(x => KiotaHost.ValidateKnownValues(x, "authentication-type", Enum.GetNames<SecuritySchemeType>()));
        return authTypeOption;
    }

    internal static Option<string> GetPluginAuthenticationReferenceIdOption(bool required = false)
    {
        var authRefIdOption = new Option<string>("--authentication-ref-id")
        {
            Description = "The authentication reference id for the plugin.",
            Required = required,
        };
        authRefIdOption.Aliases.Add("--refid");
        return authRefIdOption;
    }
    internal static Option<bool> GetNoWorkspaceOption()
    {
        var noWorkspaceOption = new Option<bool>("--no-workspace")
        {
            Description = "Disables the workspace management for the plugin.",
            Required = false,
        };
        noWorkspaceOption.Aliases.Add("--nw");
        return noWorkspaceOption;
    }
    public static Command GetAddCommand(IServiceProvider serviceProvider)
    {
        var defaultConfiguration = new GenerationConfiguration();
        var outputOption = KiotaHost.GetOutputPathOption(defaultConfiguration.OutputPath);
        var descriptionOption = KiotaHost.GetDescriptionOption(defaultConfiguration.OpenAPIFilePath, true);
        var (includePatterns, excludePatterns) = KiotaHost.GetIncludeAndExcludeOptions(defaultConfiguration.IncludePatterns, defaultConfiguration.ExcludePatterns);
        var logLevelOption = KiotaHost.GetLogLevelOption();
        var skipGenerationOption = KiotaClientCommands.GetSkipGenerationOption();
        var noWorkspaceOption = GetNoWorkspaceOption();
        var pluginNameOption = GetPluginNameOption();
        var pluginType = GetPluginTypeOption();
        var pluginAuthTypeOption = GetPluginAuthenticationTypeOption();
        var pluginAuthRefIdOption = GetPluginAuthenticationReferenceIdOption();
        var command = new Command("add", "Adds a new plugin to the Kiota configuration"){
            descriptionOption,
            includePatterns,
            excludePatterns,
            logLevelOption,
            skipGenerationOption,
            outputOption,
            pluginNameOption,
            pluginType,
            pluginAuthTypeOption,
            pluginAuthRefIdOption,
            noWorkspaceOption,
            //TODO overlay when we have support for it in OAI.net
        };
        command.Validators.Add(commandResult =>
            {
                KiotaHost.ValidateAllOrNoneOptions(commandResult, pluginAuthTypeOption, pluginAuthRefIdOption);
            });
        command.Action = new AddHandler
        {
            ClassOption = pluginNameOption,
            OutputOption = outputOption,
            PluginTypesOption = pluginType,
            PluginAuthTypeOption = pluginAuthTypeOption,
            PluginAuthRefIdOption = pluginAuthRefIdOption,
            DescriptionOption = descriptionOption,
            IncludePatternsOption = includePatterns,
            ExcludePatternsOption = excludePatterns,
            SkipGenerationOption = skipGenerationOption,
            LogLevelOption = logLevelOption,
            NoWorkspaceOption = noWorkspaceOption,
            ServiceProvider = serviceProvider,
        };
        return command;
    }
    public static Command GetEditCommand(IServiceProvider serviceProvider)
    {
        var outputOption = KiotaHost.GetOutputPathOption(string.Empty);
        var descriptionOption = KiotaHost.GetDescriptionOption(string.Empty);
        var (includePatterns, excludePatterns) = KiotaHost.GetIncludeAndExcludeOptions([], []);
        var logLevelOption = KiotaHost.GetLogLevelOption();
        var skipGenerationOption = KiotaClientCommands.GetSkipGenerationOption();
        var pluginNameOption = GetPluginNameOption();
        var pluginTypes = GetPluginTypeOption(false);
        var pluginAuthTypeOption = GetPluginAuthenticationTypeOption();
        var pluginAuthRefIdOption = GetPluginAuthenticationReferenceIdOption();
        var command = new Command("edit", "Edits a plugin configuration and updates the Kiota configuration"){
            descriptionOption,
            includePatterns,
            excludePatterns,
            logLevelOption,
            skipGenerationOption,
            outputOption,
            pluginNameOption,
            pluginTypes,
            pluginAuthTypeOption,
            pluginAuthRefIdOption,
            //TODO overlay when we have support for it in OAI.net
        };
        command.Validators.Add(commandResult =>
            {
                KiotaHost.ValidateAllOrNoneOptions(commandResult, pluginAuthTypeOption, pluginAuthRefIdOption);
            });
        command.Action = new EditHandler
        {
            ClassOption = pluginNameOption,
            OutputOption = outputOption,
            PluginTypesOption = pluginTypes,
            PluginAuthTypeOption = pluginAuthTypeOption,
            PluginAuthRefIdOption = pluginAuthRefIdOption,
            DescriptionOption = descriptionOption,
            IncludePatternsOption = includePatterns,
            ExcludePatternsOption = excludePatterns,
            SkipGenerationOption = skipGenerationOption,
            LogLevelOption = logLevelOption,
            ServiceProvider = serviceProvider,
        };
        return command;
    }
    public static Command GetRemoveCommand(IServiceProvider serviceProvider)
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
        command.Action = new RemoveHandler
        {
            ClassOption = pluginNameOption,
            CleanOutputOption = cleanOutputOption,
            LogLevelOption = logLevelOption,
            ServiceProvider = serviceProvider,
        };
        return command;
    }
    public static Command GetGenerateCommand(IServiceProvider serviceProvider)
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
        command.Action = new GenerateHandler
        {
            ClassOption = pluginNameOption,
            LogLevelOption = logLevelOption,
            RefreshOption = refreshOption,
            ServiceProvider = serviceProvider,
        };
        return command;
    }
}
