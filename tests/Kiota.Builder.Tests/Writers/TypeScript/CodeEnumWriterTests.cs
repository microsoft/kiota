using System;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.TypeScript;
using Xunit;

namespace Kiota.Builder.Tests.Writers.TypeScript;

public sealed class CodeEnumWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeEnum currentEnum;
    private const string EnumName = "someEnum";
    private readonly CodeEnumWriter codeEnumWriter;
    public CodeEnumWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, DefaultPath, DefaultName);
        codeEnumWriter = new CodeEnumWriter(new());
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        var root = CodeNamespace.InitRootNamespace();
        currentEnum = root.AddEnum(new CodeEnum
        {
            Name = EnumName,
        }).First();
        if (CodeConstant.FromCodeEnum(currentEnum) is CodeConstant constant)
        {
            currentEnum.CodeEnumObject = constant;
            root.AddConstant(constant);
        }
    }
    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }
    [Fact]
    public void WriteCodeElement_ThrowsException_WhenCodeElementIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => codeEnumWriter.WriteCodeElement(null, writer));
    }
    [Fact]
    public void WriteCodeElement_ThrowsException_WhenWriterIsNull()
    {
        var codeElement = new CodeEnum();
        Assert.Throws<ArgumentNullException>(() => codeEnumWriter.WriteCodeElement(codeElement, null));
    }
    [Fact]
    public void WritesEnum()
    {
        const string optionName = "option1";
        currentEnum.AddOption(new CodeEnumOption { Name = optionName });
        codeEnumWriter.WriteCodeElement(currentEnum, writer);
        var result = tw.ToString();
        Assert.Contains("export type SomeEnum = (", result);
        Assert.Contains("typeof", result);
        Assert.Contains(currentEnum.Name.ToFirstCharacterUpperCase(), result);
        AssertExtensions.CurlyBracesAreClosed(result, 0);
    }
    [Fact]
    public void DoesntWriteEnumWithEmptyOptions()
    {
        codeEnumWriter.WriteCodeElement(currentEnum, writer);
        var result = tw.ToString();
        Assert.Equal("", result);
    }
}
