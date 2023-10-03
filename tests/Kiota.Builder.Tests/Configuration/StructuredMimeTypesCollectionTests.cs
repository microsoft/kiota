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
        Assert.Throws<ArgumentException>(() => new StructuredMimeTypesCollection(Array.Empty<string>()));
    }
    [Fact]
    public void ParsesWithOrWithoutPriorities()
    {
        var mimeTypes = new StructuredMimeTypesCollection(new[] { "application/json", "application/xml;q=0.8" });
        Assert.Equal("application/json;q=1", mimeTypes.First(), StringComparer.OrdinalIgnoreCase);
        Assert.Equal("application/xml;q=0.8", mimeTypes.Last(), StringComparer.OrdinalIgnoreCase);
        Assert.True(mimeTypes.Contains("application/json"));
        Assert.True(mimeTypes.Contains("application/xml"));
        Assert.False(mimeTypes.Contains("application/atom+xml"));
        Assert.Null(mimeTypes.GetPriority("application/atom+xml"));
        Assert.Equal(1, mimeTypes.GetPriority("application/json"));
        Assert.Equal(0.8f, mimeTypes.GetPriority("application/xml"));
    }
}
