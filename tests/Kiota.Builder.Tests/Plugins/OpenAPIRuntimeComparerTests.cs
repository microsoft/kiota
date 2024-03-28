using Kiota.Builder.Plugins;
using Microsoft.Plugins.Manifest;
using Xunit;

namespace Kiota.Builder.Tests.Plugins;
public class OpenAPIRuntimeComparerTests
{
    private readonly OpenAPIRuntimeComparer _comparer = new();
    [Fact]
    public void Defensive()
    {
        Assert.Equal(0, _comparer.GetHashCode(null));
        Assert.True(_comparer.Equals(null, null));
        Assert.False(_comparer.Equals(new(), null));
        Assert.False(_comparer.Equals(null, new()));
    }
    [Fact]
    public void GetsHashCode()
    {
        var runtime1 = new OpenAPIRuntime { Spec = new() { { "key1", "value1" } } };
        var runtime2 = new OpenAPIRuntime { Spec = new() { { "key2", "value2" } }, Auth = new() { Type = "type" } };
        Assert.NotEqual(_comparer.GetHashCode(runtime1), _comparer.GetHashCode(runtime2));
    }
}
