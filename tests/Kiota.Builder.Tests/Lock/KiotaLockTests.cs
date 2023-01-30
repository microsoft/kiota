using Kiota.Builder.Configuration;
using Kiota.Builder.Lock;
using Xunit;

namespace Kiota.Builder.Tests.Lock;

public class KiotaLockTests
{
    [Fact]
    public void UpdatesAConfiguration()
    {
        var kiotaLock = new KiotaLock
        {
            DescriptionLocation = "description",
        };
        var generationConfiguration = new GenerationConfiguration();
        kiotaLock.UpdateGenerationConfigurationFromLock(generationConfiguration);
        Assert.Equal(kiotaLock.DescriptionLocation, generationConfiguration.OpenAPIFilePath);
    }
}
