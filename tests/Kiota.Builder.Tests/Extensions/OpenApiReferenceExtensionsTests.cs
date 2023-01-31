using Kiota.Builder.Extensions;

using Microsoft.OpenApi.Models;

using Xunit;

namespace Kiota.Builder.Tests.Extensions;
public class OpenApiReferenceExtensionsTests
{
    [Fact]
    public void GetsClassName()
    {
        var reference = new OpenApiReference
        {
            Id = "microsoft.graph.user"
        };
        Assert.Equal("User", reference.GetClassName());
    }
    [Fact]
    public void GetsClassNameDefensive()
    {
        var reference = new OpenApiReference();
        Assert.Empty(reference.GetClassName());
    }
}
