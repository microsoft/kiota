using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.OrderComparers;
using Kiota.Builder.Refiners;
using Kiota.Builder.Writers;
using Xunit;

namespace Kiota.Builder.Tests.Writers.TypeScript;

public sealed class CodeFileWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeNamespace root;
    public CodeFileWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        root = CodeNamespace.InitRootNamespace();
    }

    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void WriteCode(LanguageWriter writer, CodeElement element)
    {
        writer.Write(element);
        if (element is not CodeNamespace)
            foreach (var childElement in element.GetChildElements()
                         .Order(new CodeElementOrderComparer()))
            {
                WriteCode(writer, childElement);
            }

    }

    [Fact]
    public async Task WritesAutoGenerationStart()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var parentClass = TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root, "parentClass", true);
        TestHelper.AddSerializationPropertiesToModelClass(parentClass);
        await ILanguageRefiner.Refine(generationConfiguration, root);
        var modelsNS = root.FindChildByName<CodeNamespace>(generationConfiguration.ModelsNamespaceName);
        var codeFile = modelsNS.FindChildByName<CodeFile>("index", false);
        WriteCode(writer, codeFile);

        var result = tw.ToString();
        Assert.Contains("// eslint-disable", result);
        Assert.Contains("// tslint:disable", result);
        Assert.Contains("export function deserializeIntoParentClass", result);
        Assert.Contains("export interface ParentClass", result);
        Assert.Contains("export function serializeParentClass", result);
        Assert.Contains("// eslint-enable", result);
        Assert.Contains("// tslint:enable", result);
    }

}
