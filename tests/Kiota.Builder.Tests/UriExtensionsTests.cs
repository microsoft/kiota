using System;
using Kiota.Builder.Extensions;
using Xunit;

namespace Kiota.Builder.Tests.Writers;

public class UriExtensionsTests
{
    [Fact]
    public void Defensive()
    {
        Assert.Empty(UriExtensions.GetFileName(null));
    }
    [Fact]
    public void GetsFileName()
    {
        Assert.Equal("todo.yml", new Uri("https://contoso.com/todo.yml").GetFileName());
    }
    [Fact]
    public void StripsQueryParameters()
    {
        Assert.Equal("todo.yml", new Uri("https://contoso.com/todo.yml?foo=bar").GetFileName());
    }
}
