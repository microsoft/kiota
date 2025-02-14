using System.Text.Json.Serialization;

namespace kiota.Telemetry.Config;

public class TelemetryConfig
{
    public const string ConfigSectionKey = "Telemetry";

    [JsonPropertyName("OptOut")]
    public bool Disabled
    {
        get;
        set;
    }

    public OpenTelemetryConfig OpenTelemetry
    {
        get;
        set;
    } = new();

    public AppInsightsConfig AppInsights
    {
        get;
        set;
    } = new();
}
