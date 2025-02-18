namespace kiota.Telemetry.Config;

public class OpenTelemetryConfig
{
    public bool Enabled
    {
        get;
        set;
    }
    public string? EndpointAddress
    {
        get;
        set;
    }
}
