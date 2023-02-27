using System;

namespace Kiota.Builder.Configuration;

public class DownloadConfiguration : SearchConfigurationBase, ICloneable
{
    public string OutputPath { get; set; } = "./output/result.json";
    public bool CleanOutput
    {
        get; set;
    }

    public object Clone()
    {
        return new DownloadConfiguration
        {
            OutputPath = OutputPath,
            CleanOutput = CleanOutput,
            ClearCache = ClearCache
        };
    }
}
