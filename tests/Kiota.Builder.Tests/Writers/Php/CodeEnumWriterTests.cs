using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Php;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Php;

public sealed class CodeEnumWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeEnum currentEnum;
    private const string EnumName = "someEnum";
    private readonly CodeEnumWriter _codeEnumWriter;
    public CodeEnumWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.PHP, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        var root = CodeNamespace.InitRootNamespace();
        root.Name = "Microsoft\\Graph";
        _codeEnumWriter = new CodeEnumWriter(new PhpConventionService());
        currentEnum = root.AddEnum(new CodeEnum
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
    public async Task WritesEnumAsync()
    {
        var declaration = currentEnum.Parent as CodeNamespace;
        const string optionName = "option1";
        currentEnum.AddOption(new CodeEnumOption { Name = optionName });
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.PHP }, declaration);
        _codeEnumWriter.WriteCodeElement(currentEnum, writer);
        var result = tw.ToString();
        Assert.Contains("<?php", result);
        Assert.Contains("namespace Microsoft\\Graph;", result);
        Assert.Contains("use Microsoft\\Kiota\\Abstractions\\Enum", result);
        Assert.Contains("class", result);
        Assert.Contains("extends Enum", result);
        Assert.Contains($"public const {optionName.ToUpperInvariant()} = \"{optionName}\"", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
        Assert.Contains(optionName, result);
    }
}
