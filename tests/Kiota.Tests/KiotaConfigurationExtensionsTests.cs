using System.Collections.Generic;
using kiota;
using Kiota.Builder;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Kiota.Tests;

public sealed class KiotaConfigurationExtensionsTests
{
    [Fact]
    public void SanitizesClientNamesFromSettings()
    {
        var configurationRoot = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                [$"{nameof(KiotaConfiguration.Generation)}:{nameof(GenerationConfiguration.ClientClassName)}"] = "/tmp/Pwn { } public class INJECTED",
                [$"{nameof(KiotaConfiguration.Generation)}:{nameof(GenerationConfiguration.ClientNamespaceName)}"] = "../Microsoft.Graph\"/{/;",
                [$"{nameof(KiotaConfiguration.Languages)}:CSharp:{nameof(LanguageInformation.ClientClassName)}"] = "\\tmp\\Graph",
                [$"{nameof(KiotaConfiguration.Languages)}:CSharp:{nameof(LanguageInformation.ClientNamespaceName)}"] = "C:\\tmp\\Microsoft..Graph",
            })
            .Build();
        var kiotaConfiguration = new KiotaConfiguration();

        kiotaConfiguration.BindConfiguration(configurationRoot);

        Assert.Equal("tmpPwnpublicclassINJECTED", kiotaConfiguration.Generation.ClientClassName);
        Assert.Equal("Microsoft.Graph", kiotaConfiguration.Generation.ClientNamespaceName);
        Assert.True(kiotaConfiguration.Languages.TryGetValue("CSharp", out var languageInformation));
        Assert.Equal("tmpGraph", languageInformation.ClientClassName);
        Assert.Equal("CtmpMicrosoft.Graph", languageInformation.ClientNamespaceName);
    }
}
