using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;
using Xunit;

namespace Kiota.Builder.Tests;

public sealed partial class KiotaBuilderTests
{
    [Fact]
    public void WildcardValidationVisitsSharedAncestorsOnce()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal);
        var document = new OpenApiDocument { Components = new OpenApiComponents { Schemas = schemas } };
        var references = new List<CountingSchemaReference>();
        IOpenApiSchema parent = null;
        for (var i = 0; i < 128; i++)
        {
            var schema = new OpenApiSchema { Properties = new Dictionary<string, IOpenApiSchema> { ["keep"] = new OpenApiSchema() } };
            schemas.Add($"Model{i}", schema);
            if (parent is not null)
            {
                var link = new CountingSchemaReference($"Model{i - 1}", document, parent);
                references.Add(link);
                schema.AllOf = [link];
            }
            parent = schema;
        }

        new OpenApiPropertyFilter(NullLogger.Instance).Apply(document,
            new GenerationConfiguration { IncludeProperties = ["Model*#keep"] }, TestContext.Current.CancellationToken);

        Assert.All(references, reference => Assert.Equal(1, reference.TargetReads));
    }

    private sealed class CountingSchemaReference(string id, OpenApiDocument document, IOpenApiSchema target) :
        OpenApiSchemaReference(id, document)
    {
        internal int TargetReads
        {
            get; private set;
        }
        public override IOpenApiSchema Target
        {
            get
            {
                TargetReads++;
                return target;
            }
        }
    }

    [Fact]
    public async Task RejectsDynamicReferencesInExternalResponseSchemasAsync()
    {
        var document = await ReadPropertyFilterDocumentAsync("""
            openapi: 3.1.0
            info: {title: External response, version: 1.0.0}
            paths:
              /resource:
                get:
                  responses:
                    '200':
                      description: Placeholder for the resolved external response
            components:
              schemas:
                Resource:
                  properties: {id: {type: string}}
            """);
        var external = await ReadPropertyFilterDocumentAsync("""
            openapi: 3.1.0
            info: {title: Response definitions, version: 1.0.0}
            paths: {}
            components:
              responses:
                Resource:
                  description: OK
                  content:
                    application/json:
                      schema: {$dynamicRef: '#node'}
            """);
        var response = external.Components!.Responses!["Resource"];
        var operation = document.Paths["/resource"].Operations![System.Net.Http.HttpMethod.Get];
        operation.Responses!["200"] = new ExternalResponseReference(document, response);
        Assert.IsType<OpenApiSchemaReference>(operation.Responses!["200"].Content!["application/json"].Schema);

        var error = Assert.Throws<InvalidOperationException>(() => new OpenApiPropertyFilter(NullLogger.Instance).Apply(document,
            new GenerationConfiguration { ExcludeProperties = ["Resource#id"] }, TestContext.Current.CancellationToken));
        Assert.Contains("dynamic reference '#node'", error.Message, StringComparison.Ordinal);
        Assert.Contains("id", document.Components!.Schemas!["Resource"].Properties!.Keys);
    }

    private sealed class ExternalResponseReference(OpenApiDocument document, IOpenApiResponse response) :
        OpenApiResponseReference("Resource", document, "https://example.com/responses.yaml")
    {
        public override IOpenApiResponse Target => response;
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    public async Task RejectsTrimmingWithAmbiguousDynamicReferencesBeforeMutationAsync(bool exclude, bool hasOrdinaryReference, bool nested)
    {
        var document = await ReadPropertyFilterDocumentAsync($$$"""
            openapi: 3.1.0
            info: {title: Ambiguous dynamic reference, version: 1.0.0}
            paths: {}
            components:
              schemas:
                A:
                  $dynamicAnchor: node
                  properties: {id: {type: string}, keep: {type: string}}
                B:
                  $dynamicAnchor: node
                  properties: {id: {type: string}}
                Wrapper:
                  {{{(nested ? "$defs: {child: {$dynamicRef: '#node'}}" : "$dynamicRef: '#node'")}}}
                  {{{(hasOrdinaryReference ? "$ref: '#/components/schemas/A'" : string.Empty)}}}
                  required: [id]
            """);
        var configuration = new GenerationConfiguration
        {
            IncludeProperties = exclude ? [] : ["A#keep"],
            ExcludeProperties = exclude ? ["A#id"] : [],
        };
        var schemas = document.Components!.Schemas!;
        if (!nested)
            Assert.Equal(hasOrdinaryReference, Assert.IsType<OpenApiSchemaReference>(schemas["Wrapper"]).Target is not null);
        var filter = new OpenApiPropertyFilter(NullLogger.Instance);
        var cancellationToken = TestContext.Current.CancellationToken;
        var error = Assert.Throws<InvalidOperationException>(() => filter.Apply(document, configuration, cancellationToken));
        Assert.Contains("dynamic reference '#node'", error.Message, StringComparison.Ordinal);
        filter.Apply(document, new GenerationConfiguration(), cancellationToken);
        Assert.Contains("id", schemas["A"].Properties!.Keys);
    }

    [Theory]
    [InlineData("properties: {local: {type: string}}", "Derived#local", false)]
    [InlineData("allOf: [{properties: {local: {type: string}}}]", "Deri*#loc*", true)]
    [InlineData("anyOf: [{properties: {local: {type: string}}}]", "Derived#local", false)]
    [InlineData("oneOf: [{properties: {local: {type: string}}}]", "Derived#local", true)]
    [InlineData("allOf: [{$ref: '#/components/schemas/Base'}]", "Derived#*", false)]
    public async Task RejectsTrimmingShapeChangingReferenceSiblingsBeforeMutationAsync(string sibling, string selection, bool exclude)
    {
        var document = await ReadPropertyFilterDocumentAsync($$$"""
            openapi: 3.1.0
            info: {title: Reference siblings, version: 1.0.0}
            paths: {}
            components:
              schemas:
                Base:
                  properties: {id: {type: string}, keep: {type: string}}
                Derived:
                  $ref: '#/components/schemas/Base'
                  {{{sibling}}}
            """);
        var configuration = new GenerationConfiguration
        {
            IncludeProperties = exclude ? ["Base#keep"] : ["Base#keep", selection],
            ExcludeProperties = exclude ? [selection] : [],
        };
        var filter = new OpenApiPropertyFilter(NullLogger.Instance);
        var cancellationToken = TestContext.Current.CancellationToken;
        var error = Assert.Throws<InvalidOperationException>(() => filter.Apply(document, configuration, cancellationToken));
        Assert.Contains(selection, error.Message, StringComparison.Ordinal);
        Assert.Contains("beside $ref", error.Message, StringComparison.Ordinal);
        Assert.Contains("id", document.Components!.Schemas!["Base"].Properties!.Keys);

        filter.Apply(document, new GenerationConfiguration(), cancellationToken);
        Assert.Contains("id", document.Components.Schemas["Base"].Properties!.Keys);
    }

    [Fact]
    public async Task StillSupportsTrimmingSeparateReferenceAndInlineEntriesAsync()
    {
        var document = await ReadPropertyFilterDocumentAsync("""
            openapi: 3.1.0
            info: {title: Explicit inheritance, version: 1.0.0}
            paths: {}
            components:
              schemas:
                Base:
                  properties: {id: {type: string}}
                Derived:
                  allOf:
                    - $ref: '#/components/schemas/Base'
                    - properties: {keep: {type: string}, remove: {type: string}}
            """);
        new OpenApiPropertyFilter(NullLogger.Instance).Apply(document,
            new GenerationConfiguration { IncludeProperties = ["Derived#keep"] }, TestContext.Current.CancellationToken);
        var components = document.Components!.Schemas!;
        Assert.Contains("id", components["Base"].Properties!.Keys);
        Assert.Equal(["keep"], components["Derived"].AllOf![1].Properties!.Keys);
    }

    [Fact]
    public async Task ProtectsConstraintsDiscoveredInUnselectedReferenceSiblingsAsync()
    {
        var document = await ReadPropertyFilterDocumentAsync("""
            openapi: 3.1.0
            info: {title: Nested reference constraint, version: 1.0.0}
            paths: {}
            components:
              schemas:
                Base: {}
                User:
                  properties: {id: {type: string}}
                Wrapper:
                  $ref: '#/components/schemas/Base'
                  items:
                    allOf:
                      - $ref: '#/components/schemas/User'
                      - required: [id]
            """);
        var error = Assert.Throws<InvalidOperationException>(() => new OpenApiPropertyFilter(NullLogger.Instance).Apply(document,
            new GenerationConfiguration { ExcludeProperties = ["User#id"] }, TestContext.Current.CancellationToken));
        Assert.Contains("Cannot exclude required or discriminator property 'User#id'", error.Message, StringComparison.Ordinal);
        Assert.Contains("id", document.Components!.Schemas!["User"].Properties!.Keys);
    }
}
