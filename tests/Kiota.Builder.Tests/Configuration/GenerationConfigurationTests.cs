using Kiota.Builder.Configuration;
using Xunit;

namespace Kiota.Builder.Tests.Configuration;
public class GenerationConfigurationTests {
    [Fact]
    public void Clones() {
        var generationConfiguration = new GenerationConfiguration {
            ClientClassName = "class1",
            IncludePatterns = null,
        };
        var clone = generationConfiguration.Clone() as GenerationConfiguration;
        Assert.NotNull(clone);
        Assert.Equal(generationConfiguration.ClientClassName, clone.ClientClassName);
        Assert.NotNull(clone.IncludePatterns);
        Assert.Empty(clone.IncludePatterns);
        clone.ClientClassName = "class2";
        Assert.NotEqual(generationConfiguration.ClientClassName, clone.ClientClassName);
    }
}
