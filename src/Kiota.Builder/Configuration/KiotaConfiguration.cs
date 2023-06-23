using System;

namespace Kiota.Builder.Configuration;

#pragma warning disable CA2227
#pragma warning disable CA1002
public class KiotaConfiguration : ICloneable
{
    public GenerationConfiguration Generation { get; set; } = new();
    public SearchConfiguration Search { get; set; } = new();
    public DownloadConfiguration Download { get; set; } = new();
    public LanguagesInformation Languages { get; set; } = new();
    public UpdateConfiguration Update { get; set; } = new();

    public object Clone()
    {
        return new KiotaConfiguration
        {
            Generation = (GenerationConfiguration)Generation.Clone(),
            Search = (SearchConfiguration)Search.Clone(),
            Download = (DownloadConfiguration)Download.Clone(),
            Languages = (LanguagesInformation)Languages.Clone(),
            Update = (UpdateConfiguration)Update.Clone()
        };
    }
}
#pragma warning restore CA1002
#pragma warning restore CA2227
