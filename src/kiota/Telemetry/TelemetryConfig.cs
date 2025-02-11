using System.Text.Json.Serialization;

namespace kiota.Telemetry;

public class TelemetryConfig
{
    [JsonPropertyName("OptOut")]
    public bool Disabled
    {
        get;
        set;
    }
}
