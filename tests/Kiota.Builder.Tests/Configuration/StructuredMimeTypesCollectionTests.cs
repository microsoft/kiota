using System;
using System.Linq;
using Kiota.Builder.Configuration;
using Xunit;

namespace Kiota.Builder.Tests.Configuration;

public sealed class StructuredMimeTypesCollectionTests
{
    [Fact]
    public void Defensive()
    {
        Assert.Throws<ArgumentNullException>(() => new StructuredMimeTypesCollection(null!));
    }
    [Fact]
    public void ParsesWithOrWithoutPriorities()
    {
        var mimeTypes = new StructuredMimeTypesCollection(new[] { "application/json", "application/xml;q=0.8" });
        Assert.Equal("application/json;q=1", mimeTypes.First(), StringComparer.OrdinalIgnoreCase);
        Assert.Equal("application/xml;q=0.8", mimeTypes.Last(), StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("application/atom+xml", mimeTypes);
        Assert.Equal(0.8f, mimeTypes.GetPriority("application/atom+xml"));
        Assert.Equal(1, mimeTypes.GetPriority("application/json"));
        Assert.Equal(0.8f, mimeTypes.GetPriority("application/xml"));
    }
    [Fact]
    public void DoesNotAddDuplicates()
    {
        Assert.Throws<ArgumentException>(() => new StructuredMimeTypesCollection(new[] { "application/json", "application/json;q=0.8" }));
    }
    [Fact]
    public void ClearsEntries()
    {
        var mimeTypes = new StructuredMimeTypesCollection(new[] { "application/json", "application/xml;q=0.8" });
        Assert.Equal(2, mimeTypes.Count);
        mimeTypes.Clear();
        Assert.Empty(mimeTypes);
    }
    [Theory]
    [InlineData("application/json, application/xml, application/yaml", "application/json", "application/json;q=1")]
    [InlineData("application/json, application/xml, application/yaml", "application/json,text/plain", "application/json;q=1")]
    [InlineData("application/json, application/xml, application/yaml;q=0.8", "application/json,text/plain,application/yaml", "application/json;q=1,application/yaml;q=0.8")]
    [InlineData("application/json, application/xml;q=0.9, application/yaml;q=0.8", "application/github+json", "application/github+json;q=1")]
    [InlineData("application/vnd.topicus.keyhub+json;version=67, application/yaml;q=0.8", "application/vnd.topicus.keyhub+json;version=67", "application/vnd.topicus.keyhub+json;version=67;q=1")]
    [InlineData("application/vnd.topicus.keyhub+json, application/yaml;q=0.8", "application/vnd.topicus.keyhub+json;version=67", "application/vnd.topicus.keyhub+json;version=67;q=1")]
    public void MatchesAccept(string configuredTypes, string declaredTypes, string expectedTypes)
    {
        var mimeTypes = new StructuredMimeTypesCollection(configuredTypes.Split(',').Select(static x => x.Trim()));
        var result = mimeTypes.GetAcceptedTypes(declaredTypes.Split(',').Select(static x => x.Trim()));
        var deserializedExpectedTypes = expectedTypes.Split(',').Select(static x => x.Trim());
        foreach (var expectedType in deserializedExpectedTypes)
            Assert.Contains(expectedType, result);
    }
    [Theory]
    [InlineData("application/json, application/xml;q=0.9, application/yaml;q=0.8", "application/json", "application/json")]
    [InlineData("application/json, application/xml;q=0.9, application/yaml;q=0.8", "application/json,text/plain", "application/json")]
    [InlineData("application/json, application/xml;q=0.9, application/yaml;q=0.8", "application/json,text/plain,application/yaml", "application/json")]
    [InlineData("application/json, application/xml;q=0.9, application/yaml;q=0.8", "application/github+json", "application/github+json")]
    [InlineData("application/vnd.topicus.keyhub+json;version=67, application/yaml;q=0.8", "application/vnd.topicus.keyhub+json;version=67", "application/vnd.topicus.keyhub+json;version=67")]
    public void MatchesContentType(string configuredTypes, string declaredTypes, string expectedTypes)
    {
        var mimeTypes = new StructuredMimeTypesCollection(configuredTypes.Split(',').Select(static x => x.Trim()));
        var result = mimeTypes.GetContentTypes(declaredTypes.Split(',').Select(static x => x.Trim()));
        var deserializedExpectedTypes = expectedTypes.Split(',').Select(static x => x.Trim());
        foreach (var expectedType in deserializedExpectedTypes)
            Assert.Contains(expectedType, result);
    }
}
