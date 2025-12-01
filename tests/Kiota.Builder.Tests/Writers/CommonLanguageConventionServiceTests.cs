using System;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers.CSharp;

using Moq;

using Xunit;

namespace Kiota.Builder.Tests.Writers;

public class CommonLanguageConventionServiceTests
{
    [Fact]
    public void TranslatesType()
    {
        var service = new CSharpConventionService();
        var root = CodeNamespace.InitRootNamespace();
        var unknownTypeMock = new Mock<CodeTypeBase>();
        unknownTypeMock.Setup(x => x.Name).Returns("unknownType");
        Assert.Throws<InvalidOperationException>(() => service.TranslateType(unknownTypeMock.Object));
        var stringType = new CodeType
        {
            Name = "string"
        };
        Assert.Equal("string", service.TranslateType(stringType));
        var unionStringType = new CodeUnionType
        {
            Name = "unionString"
        };
        unionStringType.AddType(stringType);
        Assert.Equal("string", service.TranslateType(unionStringType));
    }
}
