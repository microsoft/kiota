using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using kiota.Rpc;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.CodeRenderers;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;
using Kiota.Builder.Writers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kiota.Builder.Tests.Writers.Dart;

public class DartDefaultRenderingTests
{
    [Theory]
    [InlineData("date-time", "2026-01-01T12:30:00Z", "DateTime.parse('2026-01-01T12:30:00Z')")]
    [InlineData("date", "2024-02-29", "DateOnly.fromDateTimeString('2024-02-29')")]
    [InlineData("time", "12:30:00.123456789+01:30", "TimeOnly.fromDateTimeString('12:30:00.123456789+01:30')")]
    [InlineData("uuid", "00000000-0000-0000-0000-000000000000", "UuidValue.fromString('00000000-0000-0000-0000-000000000000')")]
    [InlineData("date-time", "not-a-date", "")]
    [InlineData("date-time", "+275760-09-13T00:00:00.000001Z", "")]
    [InlineData("date", "not-a-date", "")]
    [InlineData("time", "not-a-time", "")]
    [InlineData("time", "2026-01-01T12:30:00", "")]
    [InlineData("uuid", "not-a-uuid", "")]
    [InlineData("uuid", "d4f987cf-b557-433c-9ccb-306d85f24d76\n", "")]
    [InlineData("date-time", "quote'\"line\n\r\t\\$value", "")]
    [InlineData("date", "quote'\"line\n\r\t\\$value", "")]
    [InlineData("time", "quote'\"line\n\r\t\\$value", "")]
    [InlineData("uuid", "quote'\"line\n\r\t\\$value", "")]
    public async Task ValidatesParsedSchemaDefaultsBeforeRefiningAndRendering(string format, string defaultValue, string expected)
    {
        var schema = JsonSerializer.Serialize(new { type = "string", format, @default = defaultValue });
        var description = $$"""
openapi: 3.0.3
info:
  title: Parsed defaults
  version: 1.0.0
servers:
  - url: https://example.com
paths:
  /settings:
    get:
      parameters:
        - name: filter
          in: query
          schema: {{schema}}
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
        label: {{schema}}
""";
        foreach (var configuration in new[] { false, true }.Select(usesBackingStore => new GenerationConfiguration
        {
            Language = GenerationLanguage.Dart,
            ClientNamespaceName = "client",
            UsesBackingStore = usesBackingStore,
        }))
        {
            var logger = new FakeLogger<KiotaBuilder>();
            using var client = new HttpClient();
            var builder = new KiotaBuilder(logger, configuration, client);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(description));
            var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
            var root = builder.CreateSourceModel(builder.CreateUriSpace(document));
            var query = root.FindChildByName<CodeProperty>("filter", true);
            var label = root.FindChildByName<CodeProperty>("label", true);
            Assert.NotNull(query);
            Assert.NotNull(label);
            Assert.Equal($"\"{defaultValue}\"", query.DefaultValue);
            Assert.Equal($"\"{defaultValue}\"", label.DefaultValue);
            await builder.ApplyLanguageRefinementAsync(configuration, root, TestContext.Current.CancellationToken);

            var warnings = logger.LogEntries.Where(x => x.level == LogLevel.Warning &&
                x.message.StartsWith("Ignoring the default value for property", System.StringComparison.Ordinal)).ToArray();
            Assert.Equal(string.IsNullOrEmpty(expected) ? 2 : 0, warnings.Length);
            if (string.IsNullOrEmpty(expected))
            {
                Assert.Contains(warnings, x => x.message.Contains("filter", System.StringComparison.Ordinal));
                Assert.Contains(warnings, x => x.message.Contains("label", System.StringComparison.Ordinal));
            }

            var path = Path.GetTempFileName();
            try
            {
                var writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Dart, Path.GetDirectoryName(path), "client");
                foreach (var property in new[] { query, label })
                {
                    await CodeRenderer.GetCodeRender(configuration).RenderCodeNamespaceToSingleFileAsync(writer, Assert.IsType<CodeClass>(property.Parent), path, TestContext.Current.CancellationToken);
                    var result = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
                    if (!string.IsNullOrEmpty(expected))
                        Assert.Contains(expected, result);
                    else
                    {
                        Assert.DoesNotContain("DateTime.parse(", result);
                        Assert.DoesNotContain("DateOnly.fromDateTimeString(", result);
                        Assert.DoesNotContain("TimeOnly.fromDateTimeString(", result);
                        Assert.DoesNotContain("UuidValue.fromString(", result);
                        Assert.DoesNotContain("quote", result);
                        Assert.DoesNotContain("not-a-", result);
                    }
                }
            }
            finally
            {
                File.Delete(path);
            }
        }
    }

    [Theory]
    [InlineData(false, false, "{+baseurl}/plain", "{+baseurl}/plain")]
    [InlineData(false, false, "{+baseurl}/quote\"'line\n\r\t\\$value", "{+baseurl}/quote\\\"'line\\n\\r\\t\\\\\\$value")]
    [InlineData(false, true, "{+baseurl}/plain", "{+baseurl}/plain")]
    [InlineData(false, true, "{+baseurl}/quote\"'line\n\r\t\\$value", "{+baseurl}/quote\"\\'line\\n\\r\\t\\\\\\$value")]
    [InlineData(true, false, "{+baseurl}/plain", "{+baseurl}/plain")]
    [InlineData(true, false, "{+baseurl}/quote\"'line\n\r\t\\$value", "{+baseurl}/quote\\\"'line\\n\\r\\t\\\\\\$value")]
    [InlineData(true, true, "{+baseurl}/plain", "{+baseurl}/plain")]
    [InlineData(true, true, "{+baseurl}/quote\"'line\n\r\t\\$value", "{+baseurl}/quote\"\\'line\\n\\r\\t\\\\\\$value")]
    [InlineData(false, false, "null", "null")]
    [InlineData(false, true, "null", "null")]
    [InlineData(true, false, "null", "null")]
    [InlineData(true, true, "null", "null")]
    public async Task RefinesAndRendersUrlTemplateConstructorDefaultsOnce(bool cli, bool singleQuoted, string template, string expectedContent)
    {
        var root = CodeNamespace.InitRootNamespace();
        var requestBuilder = AddRequestBuilder(root);
        if (cli)
            requestBuilder.StartBlock.Inherits = new CodeType { Name = "CliRequestBuilder", IsExternal = true };
        var quote = singleQuoted ? "'" : "\"";
        var property = requestBuilder.Properties.Single(p => p.IsOfKind(CodePropertyKind.UrlTemplate));
        property.DefaultValue = $"{quote}{template}{quote}";
        var constructor = requestBuilder.AddMethod(new CodeMethod
        {
            Name = "constructor",
            Kind = CodeMethodKind.Constructor,
            ReturnType = new CodeType { Name = "void" },
            IsAsync = false,
        }).First();
        constructor.AddParameter(new CodeParameter
        {
            Name = "pathParameters",
            Kind = CodeParameterKind.PathParameters,
            Optional = false,
            Type = new CodeType { Name = "Dictionary<string, object>", IsNullable = false },
        });
        if (!cli)
            constructor.AddParameter(new CodeParameter
            {
                Name = "requestAdapter",
                Kind = CodeParameterKind.RequestAdapter,
                Optional = false,
                Type = new CodeType { Name = "IRequestAdapter", IsNullable = false },
            });

        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Dart }, root, TestContext.Current.CancellationToken);

        Assert.Equal($"{quote}{template}{quote}", property.DefaultValue);
        var writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Dart, ".", "client");
        using var output = new StringWriter();
        writer.SetTextWriter(output);
        writer.Write(constructor);

        var adapterArgument = cli ? string.Empty : "requestAdapter, ";
        Assert.Contains($" : super({adapterArgument}{quote}{expectedContent}{quote}, pathParameters)", output.ToString());
    }

    [Theory]
    [InlineData("{+baseurl}/plain", "{+baseurl}/plain")]
    [InlineData("null", "null")]
    [InlineData("NULL", "NULL")]
    [InlineData("{+baseurl}/$count", "{+baseurl}/\\$count")]
    [InlineData("{+baseurl}/quote\"'line\n\r\t\\$value", "{+baseurl}/quote\"\\'line\\n\\r\\t\\\\\\$value")]
    public async Task RefinesAndRendersUrlTemplateOverridesOnce(string template, string expectedContent)
    {
        var root = CodeNamespace.InitRootNamespace();
        var requestBuilder = AddRequestBuilder(root);
        var method = requestBuilder.AddMethod(new CodeMethod
        {
            Name = "toGetRequestInformation",
            Kind = CodeMethodKind.RequestGenerator,
            HttpMethod = Kiota.Builder.CodeDOM.HttpMethod.Get,
            ReturnType = new CodeType { Name = "RequestInformation", IsExternal = true },
            IsAsync = false,
            UrlTemplateOverride = template,
        }).First();

        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Dart }, root, TestContext.Current.CancellationToken);

        Assert.Equal(template, method.UrlTemplateOverride);
        var writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Dart, ".", "client");
        using var output = new StringWriter();
        writer.SetTextWriter(output);
        writer.Write(method);

        Assert.Contains($"urlTemplate : '{expectedContent}',", output.ToString());
    }

    private static CodeClass AddRequestBuilder(CodeNamespace root)
    {
        var requestBuilder = root.AddNamespace("client").AddClass(new CodeClass { Name = "ItemsRequestBuilder", Kind = CodeClassKind.RequestBuilder }).First();
        requestBuilder.AddProperty(
            new CodeProperty { Name = "urlTemplate", Kind = CodePropertyKind.UrlTemplate, Type = new CodeType { Name = "string" }, DefaultValue = "\"{+baseurl}/items\"" },
            new CodeProperty { Name = "pathParameters", Kind = CodePropertyKind.PathParameters, Type = new CodeType { Name = "Dictionary<string, object>" } },
            new CodeProperty { Name = "requestAdapter", Kind = CodePropertyKind.RequestAdapter, Type = new CodeType { Name = "IRequestAdapter" } });
        return requestBuilder;
    }

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
        var nullGuard = expectedSignature == "String? id" ? "if (id != null && id.isNotEmpty)" : "if (id != null)";
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
    [InlineData(GenerationLanguage.Dart, "string", "\"null\"", "'null'")]
    [InlineData(GenerationLanguage.Dart, "string", "\"NULL\"", "'NULL'")]
    [InlineData(GenerationLanguage.Dart, "string", "\"quote'\\\"line\\n\\r\\t\\\\$value\"", "'quote\\'\"line\\n\\r\\t\\\\\\$value'")]
    [InlineData(GenerationLanguage.Dart, "integer", "42", "42")]
    [InlineData(GenerationLanguage.Dart, "integer", "\"42\"", "42")]
    [InlineData(GenerationLanguage.Dart, "boolean", "true", "true")]
    [InlineData(GenerationLanguage.Dart, "boolean", "\"TRUE\"", "true")]
    [InlineData(GenerationLanguage.CSharp, "string", "\"\"", "")]
    [InlineData(GenerationLanguage.CSharp, "string", "\"null\"", "")]
    [InlineData(GenerationLanguage.CSharp, "string", "\"NULL\"", "")]
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
    [InlineData(GenerationLanguage.Dart, false, "null")]
    [InlineData(GenerationLanguage.Dart, true, "null")]
    [InlineData(GenerationLanguage.Dart, true, "\"null\"")]
    [InlineData(GenerationLanguage.CSharp, false, "null")]
    [InlineData(GenerationLanguage.CSharp, true, "null")]
    [InlineData(GenerationLanguage.CSharp, true, "\"null\"")]
    public async Task DistinguishesSchemaNullSentinelsFromEnumWireDefaults(GenerationLanguage language, bool enumType, string defaultJson)
    {
        var schema = enumType
            ? $$"""{"type":"string","enum":["null","ready"],"default":{{defaultJson}}}"""
            : $$"""{"type":"string","default":{{defaultJson}}}""";
        var description = $$"""
openapi: 3.0.3
info:
  title: Null defaults
  version: 1.0.0
servers:
  - url: https://example.com
paths:
  /settings:
    get:
      parameters:
        - name: filter
          in: query
          schema: {{schema}}
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
        label: {{schema}}
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
        var hasDefault = language == GenerationLanguage.Dart && defaultJson != "null";
        foreach (var property in new[] { query, label })
            Assert.Equal(hasDefault ? defaultJson : string.Empty, property.DefaultValue);
        if (language != GenerationLanguage.Dart)
            return;

        await builder.ApplyLanguageRefinementAsync(configuration, root, TestContext.Current.CancellationToken);
        var path = Path.GetTempFileName();
        try
        {
            var writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Dart, Path.GetDirectoryName(path), "client");
            foreach (var property in new[] { query, label })
            {
                await CodeRenderer.GetCodeRender(configuration).RenderCodeNamespaceToSingleFileAsync(writer, Assert.IsType<CodeClass>(property.Parent), path, TestContext.Current.CancellationToken);
                var result = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
                if (hasDefault)
                {
                    var codeEnum = Assert.IsType<CodeEnum>(Assert.IsType<CodeType>(property.Type).TypeDefinition);
                    var option = codeEnum.Options.Single(x => x.WireName == "null");
                    Assert.Equal(option.Name, property.DefaultValue);
                    Assert.Contains($"{property.Name} = {codeEnum.Name}.{option.Name}", result);
                }
                else
                {
                    Assert.True(string.IsNullOrEmpty(property.DefaultValue));
                    Assert.DoesNotContain($"{property.Name} = null", result);
                    Assert.DoesNotContain($"{property.Name} = 'null'", result);
                }
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData(GenerationLanguage.Dart, """{"type":"string","default":"ready"}""", "\"ready\"", "'ready'")]
    [InlineData(GenerationLanguage.Dart, """{"allOf":[{"type":"string","default":"ready"}]}""", "\"ready\"", "'ready'")]
    [InlineData(GenerationLanguage.Dart, """{"allOf":[{"allOf":[{"type":"boolean","default":true}]}]}""", "true", "true")]
    [InlineData(GenerationLanguage.Dart, """{"allOf":[{"type":"integer","default":42}]}""", "42", "42")]
    [InlineData(GenerationLanguage.Dart, """{"allOf":[{"type":"integer","default":"42"}]}""", "42", "42")]
    [InlineData(GenerationLanguage.Dart, """{"allOf":[{"type":"string","default":"quote'\"line\n\r\t\\$value"}]}""", "\"quote'\"line\n\r\t\\$value\"", "'quote\\'\"line\\n\\r\\t\\\\\\$value'")]
    [InlineData(GenerationLanguage.Dart, """{"allOf":[{"type":"string","default":""}]}""", "\"\"", "''")]
    [InlineData(GenerationLanguage.Dart, """{"allOf":[{"type":"string","default":"null"}]}""", "\"null\"", "'null'")]
    [InlineData(GenerationLanguage.Dart, """{"allOf":[{"type":"string","default":"NULL"}]}""", "\"NULL\"", "'NULL'")]
    [InlineData(GenerationLanguage.Dart, """{"allOf":[{"type":"string","enum":["null","ready"],"default":"null"}]}""", "\"null\"", null)]
    [InlineData(GenerationLanguage.Dart, """{"allOf":[{"$ref":"#/components/schemas/Status"}]}""", "\"null\"", null)]
    [InlineData(GenerationLanguage.Dart, """{"allOf":[{"allOf":[{"$ref":"#/components/schemas/Status"}]}]}""", "\"null\"", null)]
    [InlineData(GenerationLanguage.Dart, """{"default":"ready","allOf":[{"$ref":"#/components/schemas/Status"}]}""", "\"ready\"", null)]
    [InlineData(GenerationLanguage.Dart, """{"default":"outer","allOf":[{"type":"string","default":"inner"}]}""", "\"outer\"", "'outer'")]
    [InlineData(GenerationLanguage.Dart, """{"allOf":[{"default":"middle","allOf":[{"type":"string","default":"inner"}]}]}""", "\"middle\"", "'middle'")]
    [InlineData(GenerationLanguage.Dart, """{"default":"","allOf":[{"type":"string","default":"inner"}]}""", "\"\"", "''")]
    [InlineData(GenerationLanguage.Dart, """{"default":null,"allOf":[{"type":"string","default":"inner"}]}""", "", "")]
    [InlineData(GenerationLanguage.Dart, """{"allOf":[{"default":null,"allOf":[{"type":"string","default":"inner"}]}]}""", "", "")]
    [InlineData(GenerationLanguage.Dart, """{"allOf":[{"type":"string","default":null}]}""", "", "")]
    [InlineData(GenerationLanguage.Dart, """{"allOf":[{"type":"string","enum":["null","ready"],"default":null}]}""", "", "")]
    [InlineData(GenerationLanguage.Dart, """{"default":null,"allOf":[{"$ref":"#/components/schemas/Status"}]}""", "", "")]
    [InlineData(GenerationLanguage.Dart, """{"default":"invalid","allOf":[{"type":"boolean","default":true}]}""", "", "")]
    [InlineData(GenerationLanguage.Dart, """{"allOf":[{"type":"string"}]}""", "", "")]
    [InlineData(GenerationLanguage.Dart, """{"type":"string","allOf":[{"type":"string","default":"inner"}]}""", "", "")]
    [InlineData(GenerationLanguage.Dart, """{"allOf":[{"type":"string","default":"first"},{"type":"string","default":"second"}]}""", "", "")]
    [InlineData(GenerationLanguage.Dart, """{"anyOf":[{"type":"string"}],"allOf":[{"type":"string","default":"inner"}]}""", "", "")]
    [InlineData(GenerationLanguage.Dart, """{"oneOf":[{"type":"string"}],"allOf":[{"type":"string","default":"inner"}]}""", "", "")]
    [InlineData(GenerationLanguage.Dart, """{"allOf":[{"type":"array","items":{"allOf":[{"$ref":"#/components/schemas/Status"}]}}]}""", "", "")]
    [InlineData(GenerationLanguage.CSharp, """{"allOf":[{"type":"string","default":"ready"}]}""", "", "")]
    [InlineData(GenerationLanguage.CSharp, """{"default":"outer","allOf":[{"type":"string","default":"inner"}]}""", "", "")]
    [InlineData(GenerationLanguage.CSharp, """{"allOf":[{"$ref":"#/components/schemas/Status"}]}""", "", "")]
    public async Task PreservesDefaultsAlongSingleAllOfQuerySchemaChain(GenerationLanguage language, string schema, string expectedDefault, string expectedLiteral)
    {
        var description = $$"""
openapi: 3.0.3
info:
  title: Wrapped query defaults
  version: 1.0.0
servers:
  - url: https://example.com
paths:
  /settings:
    get:
      parameters:
        - name: filter
          in: query
          schema: {{schema}}
      responses:
        '204':
          description: No content
components:
  schemas:
    Status:
      type: string
      enum: ["null", ready]
      default: "null"
""";
        var configuration = new GenerationConfiguration { Language = language, ClientNamespaceName = "client", ExcludeBackwardCompatible = true };
        using var client = new HttpClient();
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, configuration, client);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(description));
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        var root = builder.CreateSourceModel(builder.CreateUriSpace(document));
        var query = root.FindChildByName<CodeProperty>("filter", true);
        Assert.NotNull(query);
        Assert.Equal(expectedDefault, query.DefaultValue);
        if (language != GenerationLanguage.Dart)
            return;

        await builder.ApplyLanguageRefinementAsync(configuration, root, TestContext.Current.CancellationToken);
        if (expectedLiteral is null)
        {
            var codeEnum = Assert.IsType<CodeEnum>(Assert.IsType<CodeType>(query.Type).TypeDefinition);
            var wireValue = JsonSerializer.Deserialize<string>(expectedDefault);
            var option = codeEnum.Options.Single(x => x.WireName == wireValue);
            Assert.Equal(option.Name, query.DefaultValue);
            expectedLiteral = $"{codeEnum.Name}.{option.Name}";
        }
        else
            Assert.Equal(expectedDefault, query.DefaultValue);

        var path = Path.GetTempFileName();
        try
        {
            var writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Dart, Path.GetDirectoryName(path), "client");
            await CodeRenderer.GetCodeRender(configuration).RenderCodeNamespaceToSingleFileAsync(writer, Assert.IsType<CodeClass>(query.Parent), path, TestContext.Current.CancellationToken);
            var result = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            if (string.IsNullOrEmpty(expectedLiteral))
                Assert.DoesNotContain("filter =", result);
            else
                Assert.Contains($"filter = {expectedLiteral};", result);
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
