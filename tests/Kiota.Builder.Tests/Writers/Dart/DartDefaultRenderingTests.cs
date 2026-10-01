using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.CodeRenderers;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;
using Kiota.Builder.Writers;
using Xunit;

namespace Kiota.Builder.Tests.Writers.Dart;

public class DartDefaultRenderingTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task RefinesAndRendersDartDefaultsWithoutDoubleEscaping(bool inherits, bool backingStore)
    {
        var root = CodeNamespace.InitRootNamespace();
        var models = root.AddNamespace("client.models");
        var model = models.AddClass(new CodeClass { Name = "Settings", Kind = CodeClassKind.Model }).First();
        var baseModel = inherits ? models.AddClass(new CodeClass { Name = "BaseSettings", Kind = CodeClassKind.Model }).First() : model;
        if (inherits)
            model.StartBlock.Inherits = new CodeType { TypeDefinition = baseModel };
        if (backingStore)
            baseModel.AddBackingStoreProperty();
        baseModel.AddProperty(new CodeProperty
        {
            Name = "additionalData",
            Kind = CodePropertyKind.AdditionalData,
            Type = new CodeType { Name = "Dictionary<string, object>", IsExternal = true },
            DefaultValue = "{}",
        });
        const string input = "\"quote'\"line\n\r\t\\$value\"";
        var label = model.AddProperty(new CodeProperty
        {
            Name = "label",
            Kind = CodePropertyKind.Custom,
            Type = new CodeType { Name = "string", IsExternal = true },
            DefaultValue = input,
        }).First();
        model.AddProperty(
            new CodeProperty { Name = "count", Kind = CodePropertyKind.Custom, Type = new CodeType { Name = "integer", IsExternal = true }, DefaultValue = "42" },
            new CodeProperty { Name = "unsupported", Kind = CodePropertyKind.Custom, Type = new CodeType { Name = "Object", IsExternal = true }, DefaultValue = "injectedCall()" });
        var configuration = new GenerationConfiguration { Language = GenerationLanguage.Dart, UsesBackingStore = backingStore };
        await ILanguageRefiner.RefineAsync(configuration, root, TestContext.Current.CancellationToken);
        Assert.Equal(input, label.DefaultValue);

        var path = Path.GetTempFileName();
        try
        {
            var writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Dart, Path.GetDirectoryName(path), "client", backingStore);
            await CodeRenderer.GetCodeRender(configuration).RenderCodeNamespaceToSingleFileAsync(writer, model, path, TestContext.Current.CancellationToken);
            var result = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            Assert.Contains("label = 'quote\\'\"line\\n\\r\\t\\\\\\$value'", result);
            Assert.Contains("count = 42", result);
            Assert.DoesNotContain("injectedCall", result);
            Assert.DoesNotContain("unsupported =", result);
            if (backingStore)
            {
                Assert.Contains("count = 42;", result);
                Assert.Contains("label = 'quote\\'\"line\\n\\r\\t\\\\\\$value';", result);
            }
            else if (!inherits)
                Assert.Contains("additionalData = {},", result);
            if (inherits)
                Assert.Contains("super()", result);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
