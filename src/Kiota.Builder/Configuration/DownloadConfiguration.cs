namespace Kiota.Builder.Configuration;

public class DownloadConfiguration : SearchConfigurationBase
{
    public string OutputPath { get; set; } = "./output/result.json";
    public bool CleanOutput
    {
        get; set;
    }
}
