using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;
using Kiota.Builder.Writers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kiota.Builder.Tests;

public sealed partial class KiotaBuilderTests
{
    [Theory]
    [InlineData(GenerationLanguage.Dart, "anyOf")]
    [InlineData(GenerationLanguage.Dart, "oneOf")]
    [InlineData(GenerationLanguage.PHP, "anyOf")]
    [InlineData(GenerationLanguage.PHP, "oneOf")]
    public async Task ParsesEquivalentBinaryAlternativesOnceAsync(GenerationLanguage language, string composition)
    {
        var description = """
        {"openapi":"3.0.3","info":{"title":"Binary alternatives","version":"1"},
        "paths":{"/upload":{"post":{"requestBody":{"content":{"application/json":{"schema":{"$ref":"#/components/schemas/Envelope"}}}},"responses":{"204":{"description":"ok"}}}}},
        "components":{"schemas":{"Envelope":{"type":"object","properties":{"media":{"COMPOSITION":[{"type":"string","format":"binary"},{"type":"string","format":"byte"}]}}}}}}
        """.Replace("COMPOSITION", composition);
        await using var stream = await GetDocumentStreamAsync(description);
        var configuration = new GenerationConfiguration { Language = language };
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, configuration, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        var model = builder.CreateSourceModel(builder.CreateUriSpace(document));
        await ILanguageRefiner.RefineAsync(configuration, model, TestContext.Current.CancellationToken);
        var envelope = model.FindChildByName<CodeClass>("Envelope");
        Assert.NotNull(envelope);
        var media = Assert.IsType<CodeType>(Assert.Single(envelope.Properties, p => p.Name == "media").Type);
        var wrapper = Assert.IsType<CodeClass>(media.TypeDefinition);
        var factory = Assert.Single(wrapper.Methods, m => m.Kind == CodeMethodKind.Factory);
        using var output = new StringWriter();
        var writer = LanguageWriter.GetLanguageWriter(language, "./", "client");
        writer.SetTextWriter(output);
        writer.Write(factory);
        var result = output.ToString();
        Assert.DoesNotContain("else if", result);
        if (language == GenerationLanguage.Dart)
        {
            Assert.DoesNotContain(" != null", result);
            Assert.DoesNotContain(" is Iterable<int>", result);
            Assert.Contains("result.base64 = parseNode.getCollectionOfPrimitiveValues<int>();", result);
        }
        else
        {
            Assert.Contains("$parseNode->getBinaryContent() !== null", result);
            Assert.Contains("$result->setBase64(", result);
        }
    }
}
