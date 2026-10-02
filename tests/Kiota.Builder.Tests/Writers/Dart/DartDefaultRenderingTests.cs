using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.CodeRenderers;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;
using Kiota.Builder.Writers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kiota.Builder.Tests.Writers.Dart;

public class DartDefaultRenderingTests
{
    [Theory]
    [InlineData("{\"type\":\"integer\"}", "int? id")]
    [InlineData("{\"type\":\"string\",\"format\":\"uuid\"}", "UuidValue? id")]
    [InlineData("{\"type\":\"array\",\"items\":{\"type\":\"integer\"}}", "List<int>? id")]
    [InlineData("{\"type\":\"array\",\"items\":{\"type\":\"string\",\"format\":\"uuid\"}}", "List<UuidValue>? id")]
    [InlineData("{\"type\":\"string\"}", "String? id")]
    public async Task PreservesPathParametersWhenCloningSchemaGeneratedRequestBuilders(string schema, string expectedSignature)
    {
        var description = $$"""
openapi: 3.0.3
info:
  title: Clone path parameters
  version: 1.0.0
servers:
  - url: https://example.com
paths:
  /items(id={id}):
    parameters:
      - name: id
        in: path
        required: true
        schema: {{schema}}
    get:
      responses:
        '204':
          description: No content
""";
        var configuration = new GenerationConfiguration { Language = GenerationLanguage.Dart, ClientNamespaceName = "client" };
        using var client = new HttpClient();
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, configuration, client);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(description));
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        var root = builder.CreateSourceModel(builder.CreateUriSpace(document));
        await builder.ApplyLanguageRefinementAsync(configuration, root, TestContext.Current.CancellationToken);
        var requestBuilder = GetDescendants(root).OfType<CodeClass>().Single(c => c.IsOfKind(CodeClassKind.RequestBuilder) &&
            c.Methods.Any(m => m.IsOfKind(CodeMethodKind.Constructor) && m.Parameters.Any(p => p.IsOfKind(CodeParameterKind.Path))));
        var constructor = requestBuilder.Methods.Single(m => m.IsOfKind(CodeMethodKind.Constructor));
        var parameter = constructor.Parameters.Single(p => p.IsOfKind(CodeParameterKind.Path));
        Assert.True(parameter.Optional);
        Assert.True(parameter.Type.IsNullable);

        var writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Dart, ".", "client");
        using var output = new StringWriter();
        writer.SetTextWriter(output);
        writer.Write(constructor);
        var result = output.ToString();
        Assert.Contains($"(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter, {expectedSignature})", result);
        var nullGuard = expectedSignature == "String? id" ? "if (id!= null && id.isNotEmpty)" : "if (id != null)";
        Assert.Contains($"{nullGuard} pathParameters[\"id\"]=id;", result);
        Assert.DoesNotContain("List<int?>", result);
        output.GetStringBuilder().Clear();

        writer.Write(requestBuilder.Methods.Single(m => m.Name == "clone"));
        Assert.Contains($"return {requestBuilder.Name}(pathParameters, requestAdapter, null);", output.ToString());
    }

    private static IEnumerable<CodeElement> GetDescendants(CodeElement element)
    {
        foreach (var child in element.GetChildElements(true))
        {
            yield return child;
            foreach (var descendant in GetDescendants(child))
                yield return descendant;
        }
    }

    [Theory]
    [InlineData(GenerationLanguage.Dart, "string", "\"\"", "''")]
    [InlineData(GenerationLanguage.Dart, "string", "\"quote'\\\"line\\n\\r\\t\\\\$value\"", "'quote\\'\"line\\n\\r\\t\\\\\\$value'")]
    [InlineData(GenerationLanguage.Dart, "integer", "42", "42")]
    [InlineData(GenerationLanguage.Dart, "integer", "\"42\"", "42")]
    [InlineData(GenerationLanguage.Dart, "boolean", "true", "true")]
    [InlineData(GenerationLanguage.Dart, "boolean", "\"TRUE\"", "true")]
    [InlineData(GenerationLanguage.CSharp, "string", "\"\"", "")]
    [InlineData(GenerationLanguage.CSharp, "integer", "42", "42")]
    public async Task CreatesAndRendersDartSchemaDefaultsWithoutChangingOtherLanguages(GenerationLanguage language, string schemaType, string defaultJson, string expected)
    {
        var description = $$"""
openapi: 3.0.3
info:
  title: Defaults
  version: 1.0.0
servers:
  - url: https://example.com
paths:
  /settings:
    get:
      parameters:
        - name: filter
          in: query
          schema:
            type: {{schemaType}}
            default: {{defaultJson}}
      responses:
        '200':
          description: Settings
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Settings'
components:
  schemas:
    Settings:
      type: object
      properties:
        label:
          type: {{schemaType}}
          default: {{defaultJson}}
""";
        var configuration = new GenerationConfiguration { Language = language, ClientNamespaceName = "client" };
        using var client = new HttpClient();
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, configuration, client);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(description));
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        var root = builder.CreateSourceModel(builder.CreateUriSpace(document));
        var query = root.FindChildByName<CodeProperty>("filter", true);
        var label = root.FindChildByName<CodeProperty>("label", true);
        Assert.NotNull(query);
        Assert.NotNull(label);
        if (language != GenerationLanguage.Dart)
        {
            Assert.True(string.IsNullOrEmpty(query.DefaultValue));
            if (schemaType == "string")
                Assert.True(string.IsNullOrEmpty(label.DefaultValue));
            else
                Assert.Equal(defaultJson, label.DefaultValue);
            return;
        }
        var normalized = schemaType == "string" ? $"\"{JsonSerializer.Deserialize<string>(defaultJson)}\"" : expected;
        Assert.Equal(normalized, query.DefaultValue);
        Assert.Equal(normalized, label.DefaultValue);
        await builder.ApplyLanguageRefinementAsync(configuration, root, TestContext.Current.CancellationToken);

        var path = Path.GetTempFileName();
        try
        {
            var writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Dart, Path.GetDirectoryName(path), "client");
            await CodeRenderer.GetCodeRender(configuration).RenderCodeNamespaceToSingleFileAsync(writer, Assert.IsType<CodeClass>(query.Parent), path, TestContext.Current.CancellationToken);
            var result = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            Assert.Contains($"filter = {expected};", result);
            await CodeRenderer.GetCodeRender(configuration).RenderCodeNamespaceToSingleFileAsync(writer, Assert.IsType<CodeClass>(label.Parent), path, TestContext.Current.CancellationToken);
            result = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            Assert.Contains($"label = {expected}", result);
        }
        finally
        {
            File.Delete(path);
        }
    }

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
