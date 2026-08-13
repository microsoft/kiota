using System;

using Kiota.Builder.CodeDOM;

using Xunit;

namespace Kiota.Builder.Tests.CodeDOM;

public class CodePropertyTests
{
    [Fact]
    public void Defensive()
    {
        var property = new CodeProperty
        {
            Name = "prop",
            Type = new CodeType
            {
                Name = "string",
            },
        };
        Assert.False(property.IsOfKind(null));
        Assert.False(property.IsOfKind(Array.Empty<CodePropertyKind>()));
    }
    [Fact]
    public void IsOfKind()
    {
        var property = new CodeProperty
        {
            Name = "prop",
            Type = new CodeType
            {
                Name = "string",
            },
        };
        Assert.False(property.IsOfKind(CodePropertyKind.BackingStore));
        property.Kind = CodePropertyKind.RequestBuilder;
        Assert.True(property.IsOfKind(CodePropertyKind.RequestBuilder));
        Assert.True(property.IsOfKind(CodePropertyKind.RequestBuilder, CodePropertyKind.BackingStore));
        Assert.False(property.IsOfKind(CodePropertyKind.BackingStore));
    }
}
