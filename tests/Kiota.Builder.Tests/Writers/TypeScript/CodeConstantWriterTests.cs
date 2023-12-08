using System;
using System.IO;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Go;
using Kiota.Builder.Writers.TypeScript;

using Xunit;

namespace Kiota.Builder.Tests.Writers.TypeScript;
public sealed class CodeConstantWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeEnum currentEnum;
    private const string EnumName = "someEnum";
    private readonly CodeConstantWriter codeConstantWriter;

    public CodeConstantWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, DefaultPath, DefaultName);
        codeConstantWriter = new CodeConstantWriter(new());
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
        Assert.Throws<ArgumentNullException>(() => codeConstantWriter.WriteCodeElement(null, writer));
    }

    [Fact]
    public void WriteCodeElement_ThrowsException_WhenWriterIsNull()
    {
        var codeElement = new CodeConstant();
        Assert.Throws<ArgumentNullException>(() => codeConstantWriter.WriteCodeElement(codeElement, null));
    }

    [Fact]
    public void WriteCodeElement_ThrowsException_WhenOriginalCodeElementIsNull()
    {
        var codeElement = new CodeConstant();
        Assert.Throws<InvalidOperationException>(() => codeConstantWriter.WriteCodeElement(codeElement, writer));
    }

    [Fact]
    public void WritesEnumOptionDescription()
    {
        var option = new CodeEnumOption
        {
            Documentation = new()
            {
                Description = "Some option description",
            },
            Name = "option1",
        };
        currentEnum.AddOption(option);
        codeConstantWriter.WriteCodeElement(currentEnum.CodeEnumObject, writer);
        var result = tw.ToString();
        Assert.Contains($"/** {option.Documentation.Description} */", result);
        AssertExtensions.CurlyBracesAreClosed(result, 0);
    }

    [Fact]
    public void WritesEnum()
    {
        const string optionName = "option1";
        currentEnum.AddOption(new CodeEnumOption { Name = optionName });
        codeConstantWriter.WriteCodeElement(currentEnum.CodeEnumObject, writer);
        var result = tw.ToString();
        Assert.Contains("export const SomeEnumObject = {", result);
        Assert.Contains("Option1: \"option1\"", result);
        Assert.Contains("as const;", result);
        Assert.Contains(optionName, result);
        AssertExtensions.CurlyBracesAreClosed(result, 0);
    }

    [Fact]
    public void DoesntWriteAnythingOnNoOption()
    {
        codeConstantWriter.WriteCodeElement(currentEnum.CodeEnumObject, writer);
        var result = tw.ToString();
        Assert.Empty(result);
    }
}
