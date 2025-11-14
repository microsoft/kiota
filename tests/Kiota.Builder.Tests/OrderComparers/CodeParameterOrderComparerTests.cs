using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.OrderComparers;
using Kiota.Builder.Writers.Go;
using Kiota.Builder.Writers.Python;

using Moq;

using Xunit;

namespace Kiota.Builder.Tests.OrderComparers;

public class CodeParameterOrderComparerTests
{
    [Fact]
    public void DefensiveProgramming()
    {
        var comparer = new BaseCodeParameterOrderComparer();
        Assert.NotNull(comparer);
        var mockParameter = new Mock<CodeParameter>().Object;
        Assert.Equal(0, comparer.Compare(null, null));
        Assert.Equal(-1, comparer.Compare(null, mockParameter));
        Assert.Equal(1, comparer.Compare(mockParameter, null));
    }
    [Fact]
    public void PythonDefaultsBeforeNonDefaults()
    {
        var comparer = new PythonCodeParameterOrderComparer();
        Assert.NotNull(comparer);
        var param1 = new CodeParameter
        {
            Name = "param1",
            Kind = CodeParameterKind.RequestAdapter,
            Type = new CodeType
            {
                Name = "string"
            }
        };
        var param2 = new CodeParameter
        {
            Name = "param2",
            Kind = CodeParameterKind.RequestConfiguration,
            Type = new CodeType
            {
                Name = "string"
            }
        };
        Assert.Equal(110, comparer.Compare(param2, param1));
        Assert.Equal(-110, comparer.Compare(param1, param2));
        Assert.Equal(0, comparer.Compare(param2, param2));
    }
    [Fact]
    public void CancellationParameterIsAfterRequestConfigurationByDefault()
    {
        var comparer = new BaseCodeParameterOrderComparer();
        Assert.NotNull(comparer);
        var param1 = new CodeParameter
        {
            Name = "param1",
            Kind = CodeParameterKind.RequestConfiguration,
            Type = new CodeType
            {
                Name = "string"
            }
        };
        var param2 = new CodeParameter
        {
            Name = "param2",
            Kind = CodeParameterKind.Cancellation,
            Type = new CodeType
            {
                Name = "string"
            }
        };
        var parameters = new List<CodeParameter> { param1, param2 };
        Assert.Equal("param1", parameters.OrderBy(x => x, comparer).First().Name);
        Assert.Equal(110, comparer.Compare(param2, param1));
        Assert.Equal(-110, comparer.Compare(param1, param2));
        Assert.Equal(0, comparer.Compare(param2, param2));
    }
    [Fact]
    public void CancellationParameterIsAfterRequestConfigurationByDefaultWithNamesInReverseOrder()
    {
        var comparer = new BaseCodeParameterOrderComparer();
        Assert.NotNull(comparer);
        var param1 = new CodeParameter
        {
            Name = "requestConfiguration",
            Kind = CodeParameterKind.RequestConfiguration,
            Type = new CodeType
            {
                Name = "string"
            }
        };
        var param2 = new CodeParameter
        {
            Name = "cancellationToken",
            Kind = CodeParameterKind.Cancellation,
            Type = new CodeType
            {
                Name = "string"
            }
        };
        var parameters = new List<CodeParameter> { param1, param2 };
        Assert.Equal("requestConfiguration", parameters.OrderBy(x => x, comparer).First().Name);
        Assert.Equal(90, comparer.Compare(param2, param1));
        Assert.Equal(-90, comparer.Compare(param1, param2));
        Assert.Equal(0, comparer.Compare(param2, param2));
    }
    [Fact]
    public void CancellationParameterIsAfterRequestConfigurationByDefaultIfBothOptional()
    {
        var comparer = new BaseCodeParameterOrderComparer();
        Assert.NotNull(comparer);
        var param1 = new CodeParameter
        {
            Name = "param1",
            Kind = CodeParameterKind.RequestConfiguration,
            Optional = true,
            Type = new CodeType
            {
                Name = "string"
            }
        };
        var param2 = new CodeParameter
        {
            Name = "param2",
            Kind = CodeParameterKind.Cancellation,
            Optional = true,
            Type = new CodeType
            {
                Name = "string"
            }
        };
        var parameters = new List<CodeParameter> { param1, param2 };
        Assert.Equal("param1", parameters.OrderBy(x => x, comparer).First().Name);
        Assert.Equal(110, comparer.Compare(param2, param1));
        Assert.Equal(-110, comparer.Compare(param1, param2));
        Assert.Equal(0, comparer.Compare(param2, param2));
    }
    [Theory]
    [InlineData(CodeParameterKind.Path)]
    [InlineData(CodeParameterKind.RequestConfiguration)]
    [InlineData(CodeParameterKind.Serializer)]
    [InlineData(CodeParameterKind.SetterValue)]
    [InlineData(CodeParameterKind.ParseNode)]
    [InlineData(CodeParameterKind.Custom)]
    public void CancellationParameterIsBeforeOthersForGolang(CodeParameterKind testKind)
    {
        var comparer = new GoCodeParameterOrderComparer();
        Assert.NotNull(comparer);
        var param1 = new CodeParameter
        {
            Name = "param1",
            Kind = testKind,
            Type = new CodeType
            {
                Name = "string"
            }
        };
        var param2 = new CodeParameter
        {
            Name = "param2",
            Kind = CodeParameterKind.Cancellation,
            Type = new CodeType
            {
                Name = "string"
            }
        };
        var parameters = new List<CodeParameter> { param1, param2 };
        Assert.Equal("param2", parameters.OrderBy(x => x, comparer).First().Name);
        Assert.Equal(0, comparer.Compare(param2, param2));
    }
}
