using System;
using Xunit;

namespace Kiota.Builder.Tests;
public class CodeParameterTests {
    [Fact]
    public void Defensive() {
        var parameter = new CodeParameter {
            Name = "class",
        };
        Assert.False(parameter.IsOfKind((CodeParameterKind[])null));
        Assert.False(parameter.IsOfKind(Array.Empty<CodeParameterKind>()));
    }
    [Fact]
    public void IsOfKind() {
        var parameter = new CodeParameter {
            Name = "class",
        };
        Assert.False(parameter.IsOfKind(CodeParameterKind.RequestConfiguration));
        parameter.Kind = CodeParameterKind.RequestAdapter;
        Assert.True(parameter.IsOfKind(CodeParameterKind.RequestAdapter));
        Assert.True(parameter.IsOfKind(CodeParameterKind.RequestAdapter, CodeParameterKind.RequestConfiguration));
        Assert.False(parameter.IsOfKind(CodeParameterKind.RequestConfiguration));
    }
}
