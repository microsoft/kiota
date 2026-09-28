using System;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.PathSegmenters;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Ruby;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Ruby;

public sealed class CodeNamespaceWriterTests : IDisposable
{
    private const string ClientNamespaceName = "graph";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeNamespaceWriter codeElementWriter;
    private readonly CodeNamespace modelsNamespace;

    public CodeNamespaceWriterTests()
    {
        codeElementWriter = new CodeNamespaceWriter(new RubyConventionService(), new RubyPathSegmenter(Path.GetTempPath(), ClientNamespaceName));
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Ruby, "./", ClientNamespaceName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        var root = CodeNamespace.InitRootNamespace();
        modelsNamespace = root.AddNamespace($"{ClientNamespaceName}.models");
    }
    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }
    [Fact]
    public void RegistersModelsAndEnumsForAutoloadInsteadOfRequiringThem()
    {
        var baseModel = modelsNamespace.AddClass(new CodeClass { Name = "animal", Kind = CodeClassKind.Model }).First();
        var derivedModel = modelsNamespace.AddClass(new CodeClass { Name = "cat", Kind = CodeClassKind.Model }).First();
        derivedModel.StartBlock.Inherits = new CodeType { Name = "animal", TypeDefinition = baseModel };
        var colorEnum = modelsNamespace.AddEnum(new CodeEnum { Name = "color" }).First();
        colorEnum.AddOption(new CodeEnumOption { Name = "red" });
        codeElementWriter.WriteCodeElement(modelsNamespace, writer);
        var result = tw.ToString();
        Assert.DoesNotContain("require", result, StringComparison.Ordinal);
        Assert.Contains("module Graph", result, StringComparison.Ordinal);
        Assert.Contains("module Models", result, StringComparison.Ordinal);
        Assert.Contains("autoload :Animal, ::File.expand_path(\"animal\", __dir__)", result, StringComparison.Ordinal);
        Assert.Contains("autoload :Cat, ::File.expand_path(\"cat\", __dir__)", result, StringComparison.Ordinal);
        Assert.Contains("autoload :Color, ::File.expand_path(\"color\", __dir__)", result, StringComparison.Ordinal);
    }
    [Fact]
    public void DoesNotRegisterAnEnumWithoutOptions()
    {
        // the enum writer emits nothing for it, so the constant would never be defined
        modelsNamespace.AddEnum(new CodeEnum { Name = "empty" });
        codeElementWriter.WriteCodeElement(modelsNamespace, writer);
        Assert.DoesNotContain("empty", tw.ToString(), StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public void WritesNothingForANamespaceWithoutModels()
    {
        modelsNamespace.AddClass(new CodeClass { Name = "usersRequestBuilder", Kind = CodeClassKind.RequestBuilder });
        codeElementWriter.WriteCodeElement(modelsNamespace, writer);
        Assert.Empty(tw.ToString());
    }
}
