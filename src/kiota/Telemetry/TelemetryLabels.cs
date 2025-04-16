namespace kiota.Telemetry;

public static class TelemetryLabels
{
    public const string ScopeName = "microsoft.openapi.kiota";

    public const string RedactedValuePlaceholder = "REDACTED";

    public const string CommandSourceCliValue = "CLI";
    public const string CommandSourceExtensionValue = "Extension";

    // Meter instruments
    public const string InstrumentCommandDurationName = "kiota.command.duration";
    public const string InstrumentCommandExecutionsCount = "kiota.command.executions";
    public const string InstrumentClientGenerationCount = "kiota.client.generations";
    public const string InstrumentPluginGenerationCount = "kiota.plugin.generations";

    // Tags/Attributes
    public const string TagAcquisitionChannel = "kiota.acquisition_channel";
    public const string TagCommandSource = "kiota.command.source";
    public const string TagCommandName = "kiota.command.name";
    public const string TagCommandRevision = "kiota.command.revision";
    public const string TagCommandParams = "kiota.command.parameters";
    public const string TagGeneratorLanguage = "kiota.client.language";
    public const string TagGeneratorPluginTypes = "kiota.plugin.types";
    // plugin, client, manifest etc.
    public const string TagGenerationOutputType = "kiota.output_type";

    // Span/Activity names
    public const string SpanAddClientCommand = "Client/Add InvokeAsync()";
    public const string SpanEditClientCommand = "Client/Edit InvokeAsync()";
    public const string SpanGenerateClientCommand = "Client/Generate InvokeAsync()";
    public const string SpanRemoveClientCommand = "Client/Remove InvokeAsync()";

    public const string SpanAddPluginCommand = "Plugin/Add InvokeAsync()";
    public const string SpanEditPluginCommand = "Plugin/Edit InvokeAsync()";
    public const string SpanGeneratePluginCommand = "Plugin/Generate InvokeAsync()";
    public const string SpanRemovePluginCommand = "Plugin/Remove InvokeAsync()";

    public const string SpanInitWorkspaceCommand = "Workspace/Init InvokeAsync()";
    public const string SpanMigrateWorkspaceCommand = "Workspace/Migrate InvokeAsync()";

    public const string SpanDownloadCommand = "Download InvokeAsync()";
    public const string SpanGitHubDeviceLoginCommand = "Login/GitHub/Device InvokeAsync()";
    public const string SpanGitHubLogoutCommand = "Logout/GitHub InvokeAsync()";
    public const string SpanGitHubPatLoginCommand = "Login/GitHub/Pat InvokeAsync()";
    public const string SpanInfoCommand = "Info InvokeAsync()";
    public const string SpanSearchCommand = "Search InvokeAsync()";
    public const string SpanShowCommand = "Show InvokeAsync()";
    public const string SpanUpdateCommand = "Update InvokeAsync()";
}
