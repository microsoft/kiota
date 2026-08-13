using System;

using Kiota.Builder.CodeDOM;

using Xunit;

namespace Kiota.Builder.Tests.CodeDOM;

public class CodeParameterTests
{
    [Fact]
    public void Defensive()
    {
        var parameter = new CodeParameter
        {
            Name = "class",
            Type = new CodeType
            {
                Name = "string"
            }
        };
        Assert.False(parameter.IsOfKind(null));
        Assert.False(parameter.IsOfKind(Array.Empty<CodeParameterKind>()));
    }
    [Fact]
    public void IsOfKind()
    {
        var parameter = new CodeParameter
        {
            Name = "class",
            Type = new CodeType
            {
                Name = "string"
            }
        };
        Assert.False(parameter.IsOfKind(CodeParameterKind.RequestConfiguration));
        parameter.Kind = CodeParameterKind.RequestAdapter;
        Assert.True(parameter.IsOfKind(CodeParameterKind.RequestAdapter));
        Assert.True(parameter.IsOfKind(CodeParameterKind.RequestAdapter, CodeParameterKind.RequestConfiguration));
        Assert.False(parameter.IsOfKind(CodeParameterKind.RequestConfiguration));
    }
}
