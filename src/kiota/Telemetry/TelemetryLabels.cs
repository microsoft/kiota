namespace kiota.Telemetry;

public static class TelemetryLabels
{
    public const string ScopeName = "microsoft.openapi.kiota";

    public const string RedactedValuePlaceholder = "REDACTED";

    // Meter instruments
    public const string InstrumentCommandDurationName = "kiota.command.duration";
    public const string InstrumentCommandExecutionsCount = "kiota.command.executions";
    public const string InstrumentGenerationCount = "kiota.generation.count";

    // Tags/Attributes
    public const string TagCommandName = "kiota.command.name";
    public const string TagCommandRevision = "kiota.command.revision";
    public const string TagCommandParams = "kiota.command.parameters";
    public const string TagGeneratorLanguage = "kiota.language";
    // plugin, client, manifest etc.
    public const string TagGenerationOutputType = "kiota.output_type";

    // Span/Activity names
    public const string SpanAddClientCommand = "Client/Add InvokeAsync()";
    public const string SpanEditClientCommand = "Client/Edit InvokeAsync()";
    public const string SpanGenerateClientCommand = "Client/Generate InvokeAsync()";
}
