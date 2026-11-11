using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Lock;
using Xunit;

namespace Kiota.Builder.Tests.Lock;

public class KiotaLockTests
{
    [Theory]
    [InlineData("""{"language":"CSharp"}""")]
    [InlineData("""{"language":"CSharp","includeProperties":[],"excludeProperties":[]}""")]
    [InlineData("""{"language":"CSharp","includeProperties":["User#name","User#Name","User#name"],"excludeProperties":["User#mail","user#mail"]}""")]
    public async Task ReadsAndConvertsPropertySelectionsAsync(string json)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var savedLock = await new LockManagementService().GetLockFromStreamAsync(stream, TestContext.Current.CancellationToken);
        Assert.NotNull(savedLock);
        var restored = new GenerationConfiguration();
        restored.IncludeProperties.Add("Stale#name");
        restored.ExcludeProperties.Add("Stale#mail");
        savedLock.UpdateGenerationConfigurationFromLock(restored);
        var roundTrip = new KiotaLock(restored);
        var comparer = new KiotaLockComparer();
        Assert.True(comparer.Equals(savedLock, roundTrip));
        Assert.Equal(comparer.GetHashCode(savedLock), comparer.GetHashCode(roundTrip));
        Assert.DoesNotContain("Stale#name", restored.IncludeProperties);
        Assert.DoesNotContain("Stale#mail", restored.ExcludeProperties);
        Assert.Equal(json.Contains("User#name", StringComparison.Ordinal) ? 2 : 0, restored.IncludeProperties.Count);
        Assert.Equal(json.Contains("User#name", StringComparison.Ordinal) ? 2 : 0, restored.ExcludeProperties.Count);
        restored.IncludeProperties.Clear();
        restored.ExcludeProperties.Clear();
        Assert.True(comparer.Equals(savedLock, roundTrip));
    }

    [Fact]
    public void ComparesPropertySelectionsOrdinallyAndIndependentlyOfOrder()
    {
        var first = new KiotaLock();
        first.IncludeProperties.UnionWith(["User#name", "User#Name"]);
        first.ExcludeProperties.UnionWith(["User#mail", "User#id"]);
        var second = new KiotaLock();
        second.IncludeProperties.UnionWith(["User#Name", "User#name", "User#Name"]);
        second.ExcludeProperties.UnionWith(["User#id", "User#mail"]);
        var comparer = new KiotaLockComparer();
        Assert.True(comparer.Equals(first, second));
        Assert.Equal(comparer.GetHashCode(first), comparer.GetHashCode(second));
        second.IncludeProperties.Remove("User#Name");
        second.IncludeProperties.Add("user#Name");
        Assert.False(comparer.Equals(first, second));
        second.IncludeProperties = new(first.IncludeProperties, StringComparer.Ordinal);
        second.ExcludeProperties.Remove("User#id");
        second.ExcludeProperties.Add("User#Id");
        Assert.False(comparer.Equals(first, second));
    }

    [Fact]
    public void UpdatesAConfiguration()
    {
        var kiotaLock = new KiotaLock
        {
            DescriptionLocation = "description",
            AllowedExternalOrigins = ["https://contoso.com/*"],
        };
        var generationConfiguration = new GenerationConfiguration();
        kiotaLock.UpdateGenerationConfigurationFromLock(generationConfiguration);
        Assert.Equal(kiotaLock.DescriptionLocation, generationConfiguration.OpenAPIFilePath);
        Assert.Equal(kiotaLock.AllowedExternalOrigins, generationConfiguration.AllowedExternalOrigins);
    }
}
