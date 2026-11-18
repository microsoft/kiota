using System;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;
using Xunit;

namespace Kiota.Builder.Tests;

public sealed partial class KiotaBuilderTests
{
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
