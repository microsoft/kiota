using System;

namespace Kiota.Builder.Configuration;

public class KiotaConfiguration : ICloneable
{
    public GenerationConfiguration Generation { get; set; } = new();
    public SearchConfiguration Search { get; set; } = new();
    public DownloadConfiguration Download { get; set; } = new();
    public LanguagesInformation Languages { get; set; } = new();

    public object Clone()
    {
        return new KiotaConfiguration
        {
            Generation = (GenerationConfiguration)Generation.Clone(),
            Search = (SearchConfiguration)Search.Clone(),
            Download = (DownloadConfiguration)Download.Clone(),
            Languages = (LanguagesInformation)Languages.Clone()
        };
    }
}
