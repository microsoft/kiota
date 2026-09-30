using System;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Ruby;

public sealed class CodeEnumWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeEnum currentEnum;
    private const string EnumName = "someEnum";
    private readonly CodeNamespace parentNamespace;
    public CodeEnumWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Ruby, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        var root = CodeNamespace.InitRootNamespace();
        parentNamespace = root.AddNamespace("parentNamespace");
        currentEnum = parentNamespace.AddEnum(new CodeEnum
        {
            Name = EnumName,
        }).First();
    }
    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }
    [Fact]
    public void WritesEnum()
    {
        var module = currentEnum?.Parent?.Parent as CodeNamespace;
        module.Name = "testModule";
        const string optionName = "Option1";
        currentEnum.AddOption(new CodeEnumOption { Name = optionName });
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.Contains("= {", result);
        Assert.Contains(optionName, result);
        Assert.Contains("}.freeze", result);
        Assert.DoesNotContain($"{optionName}: :{optionName},", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesOnlyTheMagicCommentOnNoOption()
    {
        writer.Write(currentEnum);
        var result = tw.ToString();
        // the file still has to exist and not be empty, but defines nothing
        Assert.Equal("# frozen_string_literal: true", result.Trim());
    }
    [Fact]
    public void WritesModule()
    {
        var module = currentEnum?.Parent as CodeNamespace;
        module.Name = "testModule";
        const string optionName = "Option2";
        currentEnum.AddOption(new CodeEnumOption { Name = optionName });
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.Contains("module TestModule", result);
        Assert.Contains("Option2: \"Option2\"", result);
    }
    [Fact]
    public void WritesTheWireValueAsTheMemberValue()
    {
        currentEnum.AddOption(new CodeEnumOption { Name = "incomingPayment", SerializationName = "incoming-payment" },
                              new CodeEnumOption { Name = "read" },
                              new CodeEnumOption { Name = "Two", SerializationName = "2.0" });
        writer.Write(currentEnum);
        var result = tw.ToString();
        Assert.Contains("IncomingPayment: \"incoming-payment\",", result);
        Assert.Contains("Read: \"read\",", result);
        Assert.Contains("Two: \"2.0\"", result);
    }
}
