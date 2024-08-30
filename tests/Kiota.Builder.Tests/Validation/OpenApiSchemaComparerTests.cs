using System;
using Kiota.Builder.Validation;
using Xunit;

namespace Kiota.Builder.Tests.Validation;

public class OpenApiSchemaComparerTests
{
    private readonly OpenApiSchemaComparer _comparer = new();
    [Fact]
    public void Defensive()
    {
        Assert.Equal(new HashCode().ToHashCode(), _comparer.GetHashCode(null));
        Assert.True(_comparer.Equals(null, null));
        Assert.False(_comparer.Equals(new(), null));
        Assert.False(_comparer.Equals(null, new()));
    }

    [Fact]
    public void TestEquals()
    {
        Assert.True(_comparer.Equals(new(), new()));
    }
}
