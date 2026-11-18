using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.OpenApiExtensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;
using Xunit;

#nullable enable

namespace Kiota.Builder.Tests;

public sealed partial class KiotaBuilderTests
{
    private static string ReferenceOwnedPropertiesDescription(bool inlineReference)
    {
        const string reference = """
            {
              "$ref": "#/components/schemas/Base",
              "properties": {
                "keep": { "type": "string" },
                "remove": { "type": "string" },
                "shared": { "type": "string" }
              },
              "allOf": [{ "properties": { "inline": { "type": "string" } } }]
            }
            """;
        return $$"""
            {
              "openapi": "3.1.0",
              "info": { "title": "Reference-owned properties", "version": "1.0.0" },
              "servers": [{ "url": "https://example.com" }],
              "paths": {
                "/derived": { "get": { "responses": { "200": {
                  "description": "OK",
                  "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Derived" } } }
                } } } },
                "/base": { "get": { "responses": { "200": {
                  "description": "OK",
                  "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Base" } } }
                } } } }
              },
              "components": {
                "schemas": {
                  "Base": {
                    "type": "object",
                    "properties": {
                      "inherited": { "type": "string" },
                      "shared": { "type": "string" }
                    }
                  },
                  "Derived": {{(inlineReference ? "{\"allOf\":[" + reference + "]}" : reference)}}
                }
              }
            }
            """;
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public async Task FiltersOnlyAuthoredReferenceSiblingDeclarationsAsync(bool inlineReference, bool exclude, bool wildcard)
    {
        var document = await ReadPropertyFilterDocumentAsync(ReferenceOwnedPropertiesDescription(inlineReference));
        var derived = document.Components!.Schemas!["Derived"];
        var reference = Assert.IsType<OpenApiSchemaReference>(inlineReference ? derived.AllOf![0] : derived);
        var baseSchema = document.Components.Schemas["Base"];
        var originalBaseProperties = baseSchema.Properties!.ToArray();
        new OpenApiPropertyFilter(NullLogger.Instance).Apply(document, new GenerationConfiguration
        {
            IncludeProperties = exclude ? [] : [wildcard ? "Deri*#ke*" : "Derived#keep"],
            ExcludeProperties = exclude ? [wildcard ? "Deri*#rem*" : "Derived#remove"] : [],
        });
        Assert.Contains("keep", reference.Reference.Properties!.Keys);
        Assert.DoesNotContain("remove", reference.Reference.Properties.Keys);
        Assert.Equal(exclude, reference.Reference.Properties.ContainsKey("shared"));
        Assert.Equal(exclude, reference.Reference.AllOf![0].Properties!.ContainsKey("inline"));
        Assert.Equal(originalBaseProperties, baseSchema.Properties!.ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReferenceTargetsRemainOwnedByTheirDeclaringComponentAsync(bool inlineReference)
    {
        var document = await ReadPropertyFilterDocumentAsync(ReferenceOwnedPropertiesDescription(inlineReference));
        var exception = Assert.Throws<InvalidOperationException>(() => new OpenApiPropertyFilter(NullLogger.Instance)
            .Apply(document, new GenerationConfiguration { IncludeProperties = ["Derived#inherited"] }));
        Assert.Contains("declaring component: Base.", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("allOf")]
    [InlineData("anyOf")]
    [InlineData("oneOf")]
    public async Task ReferenceSiblingCompositionFragmentsBelongToTheirComponentAsync(string composition)
    {
        var document = await ReadPropertyFilterDocumentAsync($$"""
            openapi: 3.1.0
            info:
              title: Reference-owned composition
              version: 1.0.0
            paths: {}
            components:
              schemas:
                Base:
                  type: object
                  properties:
                    inherited:
                      type: string
                Derived:
                  $ref: '#/components/schemas/Base'
                  {{composition}}:
                    - type: object
                      properties:
                        keep:
                          type: string
                        remove:
                          type: string
            """);
        var reference = Assert.IsType<OpenApiSchemaReference>(document.Components!.Schemas!["Derived"]);
        var fragment = (reference.Reference.AllOf ?? reference.Reference.AnyOf ?? reference.Reference.OneOf)![0];
        new OpenApiPropertyFilter(NullLogger.Instance).Apply(document, new GenerationConfiguration { IncludeProperties = ["Derived#keep"] });
        Assert.Contains("keep", fragment.Properties!.Keys);
        Assert.DoesNotContain("remove", fragment.Properties.Keys);
        Assert.Contains("inherited", document.Components.Schemas["Base"].Properties!.Keys);
    }

    [Theory]
    [InlineData("id")]
    [InlineData("shared")]
    [InlineData("kind")]
    public async Task ReferenceOwnedRequiredAndDiscriminatorFieldsRejectExclusionsAsync(string property)
    {
        var document = await ReadPropertyFilterDocumentAsync(ReferenceOwnedPropertiesDescription(false));
        var reference = Assert.IsType<OpenApiSchemaReference>(document.Components!.Schemas!["Derived"]);
        reference.Reference.Required = new HashSet<string>(StringComparer.Ordinal) { "id" };
        reference.Reference.Discriminator = new OpenApiDiscriminator { PropertyName = "kind" };
        reference.Reference.Properties!["id"] = new OpenApiSchema { Type = JsonSchemaType.String };
        reference.Reference.Properties["kind"] = new OpenApiSchema { Type = JsonSchemaType.String };
        Assert.IsType<OpenApiSchema>(document.Components.Schemas["Base"]).Required = new HashSet<string>(StringComparer.Ordinal) { "shared" };
        var exception = Assert.Throws<InvalidOperationException>(() => new OpenApiPropertyFilter(NullLogger.Instance)
            .Apply(document, new GenerationConfiguration { ExcludeProperties = [$"Derived#{property}"] }));
        Assert.StartsWith("Cannot exclude required or discriminator property", exception.Message, StringComparison.Ordinal);
        Assert.Contains(property, reference.Reference.Properties.Keys);
    }

    [Fact]
    public async Task TrimmingFinalReferenceOwnedFieldPreservesLocalShapeWithoutMutatingTargetAsync()
    {
        var document = await ReadPropertyFilterDocumentAsync(ReferenceOwnedPropertiesDescription(false));
        var reference = Assert.IsType<OpenApiSchemaReference>(document.Components!.Schemas!["Derived"]);
        reference.Reference.Properties!.Remove("keep");
        reference.Reference.Properties.Remove("shared");
        reference.Reference.AllOf = null;
        var baseSchema = Assert.IsType<OpenApiSchema>(document.Components.Schemas["Base"]);
        baseSchema.Extensions = new Dictionary<string, IOpenApiExtension>(StringComparer.Ordinal)
        {
            ["x-retained"] = OpenApiKiotaTrimmedPropertiesExtension.Instance,
        };
        var originalExtensions = baseSchema.Extensions;
        new OpenApiPropertyFilter(NullLogger.Instance).Apply(document, new GenerationConfiguration { ExcludeProperties = ["Derived#remove"] });
        Assert.Empty(reference.Reference.Properties);
        Assert.True(reference.HasPropertyInformation());
        Assert.Contains(OpenApiKiotaTrimmedPropertiesExtension.Name, reference.Reference.Extensions!.Keys);
        Assert.Contains("x-retained", reference.Extensions!.Keys);
        Assert.Same(originalExtensions, baseSchema.Extensions);
        Assert.DoesNotContain(OpenApiKiotaTrimmedPropertiesExtension.Name, baseSchema.Extensions.Keys);
        Assert.Equal(2, baseSchema.Properties!.Count);
        var alias = new OpenApiSchemaReference("Derived", document)
        {
            Extensions = new Dictionary<string, IOpenApiExtension>(StringComparer.Ordinal)
            {
                ["x-alias"] = OpenApiKiotaTrimmedPropertiesExtension.Instance,
            },
        };
        Assert.True(alias.HasPropertyInformation());
    }

    [Fact]
    public async Task ModelsUsedByReferenceSiblingPropertiesAreReportedAsReachableAsync()
    {
        var document = await ReadPropertyFilterDocumentAsync(ReferenceOwnedPropertiesDescription(false));
        document.AddComponent("Profile", new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["keep"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["remove"] = new OpenApiSchema { Type = JsonSchemaType.String },
            },
        });
        var reference = Assert.IsType<OpenApiSchemaReference>(document.Components!.Schemas!["Derived"]);
        reference.Reference.Properties!["profile"] = new OpenApiSchemaReference("Profile", document);
        var messages = new List<string>();
        new OpenApiPropertyFilter(new RecordingLogger(messages)).Apply(document,
            new GenerationConfiguration { IncludeProperties = ["Profile#keep"] });
        Assert.DoesNotContain(messages, static message => message.Contains("not referenced by the selected operations", StringComparison.Ordinal));
        Assert.DoesNotContain("remove", document.Components.Schemas["Profile"].Properties!.Keys);
    }

    [Theory]
    [InlineData(GenerationLanguage.CSharp, "cs", false)]
    [InlineData(GenerationLanguage.CSharp, "cs", true)]
    [InlineData(GenerationLanguage.TypeScript, "ts", false)]
    [InlineData(GenerationLanguage.TypeScript, "ts", true)]
    public async Task GeneratesFilteredReferenceSiblingFieldsAsync(GenerationLanguage language, string extension, bool inlineReference)
    {
        var directory = Directory.CreateTempSubdirectory("kiota-reference-properties-").FullName;
        try
        {
            var description = Path.Combine(directory, "api.yaml");
            await File.WriteAllTextAsync(description, ReferenceOwnedPropertiesDescription(inlineReference), TestContext.Current.CancellationToken);
            var output = Path.Combine(directory, "client");
            var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, new GenerationConfiguration
            {
                OpenAPIFilePath = description,
                OutputPath = output,
                Language = language,
                NoWorkspace = true,
                IncludeProperties = ["Derived#keep"],
            }, _httpClient);
            Assert.True(await builder.GenerateClientAsync(TestContext.Current.CancellationToken));
            var files = Directory.GetFiles(output, $"*.{extension}", SearchOption.AllDirectories);
            var source = string.Join(Environment.NewLine, await Task.WhenAll(files.Select(path => File.ReadAllTextAsync(path, TestContext.Current.CancellationToken))));
            Assert.Contains("\"keep\"", source, StringComparison.Ordinal);
            Assert.Contains("\"inherited\"", source, StringComparison.Ordinal);
            Assert.DoesNotContain("\"remove\"", source, StringComparison.Ordinal);
            Assert.DoesNotContain("\"inline\"", source, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
