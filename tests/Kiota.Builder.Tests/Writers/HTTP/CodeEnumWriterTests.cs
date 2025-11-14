using System;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Http;
using Xunit;

namespace Kiota.Builder.Tests.Writers.Http;

public sealed class CodeEnumWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeEnum currentEnum;
    private const string EnumName = "someEnum";
    private readonly GenericCodeElementWriter codeEnumWriter;
    public CodeEnumWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.HTTP, DefaultPath, DefaultName);
        codeEnumWriter = new GenericCodeElementWriter(new HttpConventionService());
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
    public void SkipsEnum()
    {
        const string optionName = "option1";
        currentEnum.AddOption(new CodeEnumOption { Name = optionName });
        codeEnumWriter.WriteCodeElement(currentEnum, writer);
        var result = tw.ToString();
        Assert.True(string.IsNullOrEmpty(result));
    }
}
