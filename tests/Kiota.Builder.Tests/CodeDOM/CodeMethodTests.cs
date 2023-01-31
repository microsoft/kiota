using System;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

using Xunit;

namespace Kiota.Builder.Tests.CodeDOM;
public class CodeMethodTests
{
    [Fact]
    public void Defensive()
    {
        var method = new CodeMethod
        {
            Name = "class",
            ReturnType = new CodeType
            {
                Name = "string"
            }
        };
        Assert.False(method.IsOfKind(null));
        Assert.False(method.IsOfKind(Array.Empty<CodeMethodKind>()));
        Assert.Throws<ArgumentNullException>(() => method.AddErrorMapping(null, new CodeType { Name = "class" }));
        Assert.Throws<ArgumentNullException>(() => method.AddErrorMapping("oin", null));
        Assert.Throws<ArgumentNullException>(() => method.ReturnType = null);
    }
    [Fact]
    public void IsOfKind()
    {
        var method = new CodeMethod
        {
            Name = "class",
            ReturnType = new CodeType
            {
                Name = "string"
            }
        };
        Assert.False(method.IsOfKind(CodeMethodKind.Constructor));
        method.Kind = CodeMethodKind.Deserializer;
        Assert.True(method.IsOfKind(CodeMethodKind.Deserializer));
        Assert.True(method.IsOfKind(CodeMethodKind.Deserializer, CodeMethodKind.Getter));
        Assert.False(method.IsOfKind(CodeMethodKind.Getter));
    }
    [Fact]
    public void AddsParameter()
    {
        var method = new CodeMethod
        {
            Name = "method1",
            ReturnType = new CodeType
            {
                Name = "string"
            }
        };
        Assert.Throws<ArgumentNullException>(() =>
        {
            method.AddParameter((CodeParameter)null);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            method.AddParameter(null);
        });
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            method.AddParameter(Array.Empty<CodeParameter>());
        });
    }
    [Fact]
    public void ClonesParameters()
    {
        var method = new CodeMethod
        {
            Name = "method1",
            ReturnType = new CodeType
            {
                Name = "string"
            }
        };
        method.AddParameter(new CodeParameter
        {
            Name = "param1",
            Type = new CodeType
            {
                Name = "string"
            }
        });
        var clone = method.Clone() as CodeMethod;
        Assert.Equal(method.Name, clone.Name);
        Assert.Single(method.Parameters);
        Assert.Equal(method.Parameters.First().Name, clone.Parameters.First().Name);
    }
    [Fact]
    public void ParametersExtensionsReturnsValue()
    {
        var method = new CodeMethod
        {
            Name = "method1",
            ReturnType = new CodeType
            {
                Name = "string"
            }
        };
        method.AddParameter(new CodeParameter
        {
            Name = "param1",
            Kind = CodeParameterKind.Custom,
            Type = new CodeType
            {
                Name = "string"
            }
        });
        Assert.NotNull(method.Parameters.OfKind(CodeParameterKind.Custom));
        Assert.Null(method.Parameters.OfKind(CodeParameterKind.RequestBody));
    }
}
