namespace kiota.Telemetry.Config;

public class AppInsightsConfig
{
    public bool Enabled
    {
        get;
        set;
    }
    public string? ConnectionString
    {
        get;
        set;
    }
}
