using Kiota.Builder.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using static kiota.ServiceConstants;

namespace kiota;

internal class CliOverrides(
    IOptionsMonitor<KiotaConfiguration> kiotaConfiguration,
    [FromKeyedServices(ServiceKeys.Default)]
    GenerationConfiguration generationConfiguration
)
{
    public bool? DisableSslValidation
    {
        get;
        set;
    }

    public string? OutputPath
    {
        get;
        set;
    }

    internal bool GetEffectiveDisableSslValidation()
    {
        return this.DisableSslValidation ?? kiotaConfiguration.CurrentValue?.Generation.DisableSSLValidation ??
            generationConfiguration.DisableSSLValidation;
    }
    internal string GetEffectiveOutputPath()
    {
        return this.OutputPath ?? kiotaConfiguration.CurrentValue?.Generation.OutputPath ??
            generationConfiguration.OutputPath;
    }
}
