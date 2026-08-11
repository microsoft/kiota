using System;
using Kiota.Builder.Plugins;
using Microsoft.DeclarativeAgents.Manifest;
using Xunit;

namespace Kiota.Builder.Tests.Plugins;

public class OpenAPIRuntimeComparerTests
{
    private readonly OpenAPIRuntimeComparer _comparer = new();
    [Fact]
    public void Defensive()
    {
        Assert.Equal(new HashCode().ToHashCode(), _comparer.GetHashCode(null));
        Assert.True(_comparer.Equals(null, null));
        Assert.False(_comparer.Equals(new(), null));
        Assert.False(_comparer.Equals(null, new()));
    }
    [Fact]
    public void GetsHashCode()
    {
        var runtime1 = new OpenApiRuntime { Spec = new() { Url = "url", ApiDescription = "description" } };
        var runtime2 = new OpenApiRuntime { Spec = new() { Url = "url", ApiDescription = "description" }, Auth = new AnonymousAuth() };
        Assert.NotEqual(_comparer.GetHashCode(runtime1), _comparer.GetHashCode(runtime2));
    }
}
