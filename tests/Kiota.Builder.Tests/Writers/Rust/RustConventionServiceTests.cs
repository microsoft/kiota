using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers.Rust;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Rust;

public class RustConventionServiceTests
{
    private readonly RustConventionService sut = new();

    [Fact]
    public void TranslatesStringType()
    {
        var codeType = new CodeType { Name = "string" };
        var result = sut.TranslateType(codeType);
        Assert.Equal("String", result);
    }
    [Fact]
    public void TranslatesIntegerType()
    {
        var codeType = new CodeType { Name = "integer" };
        var result = sut.TranslateType(codeType);
        Assert.Equal("i32", result);
    }
    [Fact]
    public void TranslatesBooleanType()
    {
        var codeType = new CodeType { Name = "boolean" };
        var result = sut.TranslateType(codeType);
        Assert.Equal("bool", result);
    }
    [Fact]
    public void TranslatesDateTimeType()
    {
        var codeType = new CodeType { Name = "DateTimeOffset" };
        var result = sut.TranslateType(codeType);
        Assert.Equal("chrono::DateTime<chrono::FixedOffset>", result);
    }
    [Fact]
    public void GetAccessModifierPublic()
    {
        var result = sut.GetAccessModifier(AccessModifier.Public);
        Assert.Equal("pub ", result);
    }
    [Fact]
    public void GetAccessModifierPrivate()
    {
        var result = sut.GetAccessModifier(AccessModifier.Private);
        Assert.Equal(string.Empty, result);
    }
    [Fact]
    public void TranslatesVoidType()
    {
        var codeType = new CodeType { Name = "void" };
        var result = sut.TranslateType(codeType);
        Assert.Equal("()", result);
    }
    [Fact]
    public void TranslatesGuidType()
    {
        var codeType = new CodeType { Name = "guid" };
        var result = sut.TranslateType(codeType);
        Assert.Equal("uuid::Uuid", result);
    }
    [Fact]
    public void TranslatesBinaryType()
    {
        var codeType = new CodeType { Name = "binary" };
        var result = sut.TranslateType(codeType);
        Assert.Equal("Vec<u8>", result);
    }
    [Fact]
    public void StreamTypeNameIsCorrect()
    {
        Assert.Equal("Vec<u8>", sut.StreamTypeName);
    }
    [Fact]
    public void VoidTypeNameIsCorrect()
    {
        Assert.Equal("()", sut.VoidTypeName);
    }
    [Fact]
    public void DocCommentPrefixIsCorrect()
    {
        Assert.Equal("/// ", sut.DocCommentPrefix);
    }
}
