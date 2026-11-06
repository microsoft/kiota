using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests;

public sealed partial class KiotaBuilderTests
{
    private const string SchemaReferenceOrderDescription = """
openapi: 3.1.0
info:
  title: repro
  version: 1.0.0
servers:
- url: https://example.com/api
paths:
  /root:
    get:
      responses:
        '200':
          description: OK
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Container'
components:
  schemas:
    Container:
      type: object
      properties:
        # test
        aliasFirst:
          $ref: '#/components/schemas/ModelAlias'
        modelSecond:
          $ref: '#/components/schemas/Model'
    Model:
      type: object
      properties:
        name:
          type: string
    ModelAlias:
      allOf:
      - $ref: '#/components/schemas/Model'
""";

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task CreatesModelFactoriesRegardlessOfAliasReferenceOrderAsync(bool reverseProperties, bool chainedAlias)
    {
        var description = SchemaReferenceOrderDescription;
        if (reverseProperties)
            description = description.Replace("""
        aliasFirst:
          $ref: '#/components/schemas/ModelAlias'
        modelSecond:
          $ref: '#/components/schemas/Model'
""", """
        modelSecond:
          $ref: '#/components/schemas/Model'
        aliasFirst:
          $ref: '#/components/schemas/ModelAlias'
""", StringComparison.Ordinal);
        if (chainedAlias)
            description = description.Replace("""
    ModelAlias:
      allOf:
      - $ref: '#/components/schemas/Model'
""", """
    ModelAlias:
      allOf:
      - $ref: '#/components/schemas/IntermediateAlias'
    IntermediateAlias:
      allOf:
      - $ref: '#/components/schemas/Model'
""", StringComparison.Ordinal);

        await using var stream = await GetDocumentStreamAsync(description);
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, new GenerationConfiguration { Language = GenerationLanguage.CSharp }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        var model = builder.CreateSourceModel(builder.CreateUriSpace(document));
        var container = model.FindChildByName<CodeClass>("Container");
        Assert.NotNull(container);
        foreach (var name in new[] { "aliasFirst", "modelSecond" })
        {
            var property = Assert.Single(container.Properties, x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            var definition = Assert.IsType<CodeClass>(Assert.IsType<CodeType>(property.Type).TypeDefinition);
            AssertModelFactory(definition);
        }
        AssertModelFactory(container);
        var models = model.FindChildByName<CodeNamespace>("ApiSdk.models");
        Assert.NotNull(models);
        foreach (var definition in models.Classes)
        {
            AssertModelFactory(definition);
            foreach (var mapping in definition.DiscriminatorInformation.DiscriminatorMappings)
                Assert.True(Assert.IsType<CodeClass>(Assert.IsType<CodeType>(mapping.Value).TypeDefinition).DerivesFrom(definition));
        }
    }

    [Theory]
    [InlineData(false, 1, false, false)]
    [InlineData(true, 1, false, false)]
    [InlineData(false, 3, false, false)]
    [InlineData(true, 3, false, false)]
    [InlineData(false, 1, true, false)]
    [InlineData(true, 1, true, false)]
    [InlineData(false, 3, true, false)]
    [InlineData(true, 3, true, false)]
    [InlineData(true, 1, false, true)]
    [InlineData(true, 3, true, true)]
    public async Task PreservesDiscriminatedInheritanceWithAliasesAndCyclesAsync(bool derivedFirst, int parallelism, bool recursiveProperties, bool crlfSource)
    {
        var description = """
openapi: 3.1.0
info:
  title: recursive aliases
  version: 1.0.0
paths:
  /root:
    get:
      responses:
        '200':
          description: OK
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Container'
components:
  schemas:
    Container:
      type: object
      properties:
        first:
          $ref: '#/components/schemas/ModelAlias'
        second:
          $ref: '#/components/schemas/Model'
        derived:
          $ref: '#/components/schemas/Derived'
    Model:
      type: object
      discriminator:
        propertyName: kind
        mapping:
          model: '#/components/schemas/Model'
          derived: '#/components/schemas/Derived'
          leaf: '#/components/schemas/Leaf'
      properties:
        kind:
          type: string
        self:
          $ref: '#/components/schemas/Model'
        alias:
          $ref: '#/components/schemas/ModelAlias'
    ModelAlias:
      allOf:
      - $ref: '#/components/schemas/Model'
    Derived:
      allOf:
      - $ref: '#/components/schemas/Model'
      type: object
      discriminator:
        propertyName: kind
        mapping:
          leaf: '#/components/schemas/Leaf'
      properties:
        parent:
          $ref: '#/components/schemas/Model'
    Leaf:
      allOf:
      - $ref: '#/components/schemas/Derived'
      type: object
      properties:
        related:
          $ref: '#/components/schemas/Derived'
""";
        if (crlfSource)
            description = description.ReplaceLineEndings("\r\n");
        description = description.ReplaceLineEndings("\n");
        if (!recursiveProperties)
            description = description.Replace("""
        self:
          $ref: '#/components/schemas/Model'
        alias:
          $ref: '#/components/schemas/ModelAlias'
""", string.Empty, StringComparison.Ordinal);
        if (derivedFirst)
        {
            description = description.Replace("first:\n          $ref: '#/components/schemas/ModelAlias'", "first:\n          $ref: '#/components/schemas/Derived'", StringComparison.Ordinal)
                .Replace("derived:\n          $ref: '#/components/schemas/Derived'", "alias:\n          $ref: '#/components/schemas/ModelAlias'", StringComparison.Ordinal);
            Assert.Contains("first:\n          $ref: '#/components/schemas/Derived'", description);
            Assert.DoesNotContain("derived:\n          $ref: '#/components/schemas/Derived'", description);
        }
        await using var stream = await GetDocumentStreamAsync(description);
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, new GenerationConfiguration { Language = GenerationLanguage.CSharp, MaxDegreeOfParallelism = parallelism }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        // Several paths share the same classes, exercising concurrent class publication as well as reentry.
        for (var i = 0; i < 8; i++)
            document.Paths.Add($"/other{i}", document.Paths["/root"]);
        for (var generation = 0; generation < 2; generation++)
        {
            var model = builder.CreateSourceModel(builder.CreateUriSpace(document));
            var baseClass = model.FindChildByName<CodeClass>("Model");
            var alias = model.FindChildByName<CodeClass>("ModelAlias");
            var derived = model.FindChildByName<CodeClass>("Derived");
            var leaf = model.FindChildByName<CodeClass>("Leaf");
            Assert.NotNull(baseClass);
            Assert.NotNull(alias);
            Assert.NotNull(derived);
            Assert.NotNull(leaf);
            foreach (var definition in new[] { baseClass, alias, derived, leaf })
                AssertModelFactory(definition);
            Assert.Same(baseClass, derived.BaseClass);
            Assert.Same(derived, leaf.BaseClass);
            Assert.Equal("kind", baseClass.DiscriminatorInformation.DiscriminatorPropertyName);
            Assert.Equal(new[] { "derived", "leaf" }, baseClass.DiscriminatorInformation.DiscriminatorMappings.Select(static x => x.Key).Order().ToArray());
            Assert.Same(derived, Assert.IsType<CodeType>(baseClass.DiscriminatorInformation.GetDiscriminatorMappingValue("derived")).TypeDefinition);
            Assert.Same(leaf, Assert.IsType<CodeType>(baseClass.DiscriminatorInformation.GetDiscriminatorMappingValue("leaf")).TypeDefinition);
            Assert.Equal("kind", derived.DiscriminatorInformation.DiscriminatorPropertyName);
            Assert.Equal("leaf", Assert.Single(derived.DiscriminatorInformation.DiscriminatorMappings).Key);
            Assert.Same(leaf, Assert.IsType<CodeType>(derived.DiscriminatorInformation.GetDiscriminatorMappingValue("leaf")).TypeDefinition);
            Assert.Empty(alias.DiscriminatorInformation.DiscriminatorMappings);
            Assert.Empty(leaf.DiscriminatorInformation.DiscriminatorMappings);
            if (recursiveProperties)
            {
                var self = Assert.Single(baseClass.Properties, static x => x.Name == "self");
                Assert.Same(baseClass, Assert.IsType<CodeType>(self.Type).TypeDefinition);
            }
        }
    }

    [Fact]
    public async Task CompletesFactoryAndMappingsAfterSameClassReentryAsync()
    {
        await using var stream = await GetDocumentStreamAsync(SchemaReferenceOrderDescription);
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, new GenerationConfiguration { Language = GenerationLanguage.CSharp }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        var node = builder.CreateUriSpace(document);
        var root = builder.CreateSourceModel(null);
        var models = root.FindChildByName<CodeNamespace>("ApiSdk.models");
        Assert.NotNull(models);
        document.AddComponent("Derived", new OpenApiSchema
        {
            AllOf = [new OpenApiSchemaReference("Model", document)],
            Properties = new Dictionary<string, IOpenApiSchema> { ["value"] = new OpenApiSchema { Type = JsonSchemaType.String } },
        });
        var discriminator = new OpenApiDiscriminator
        {
            PropertyName = "kind",
            Mapping = new Dictionary<string, OpenApiSchemaReference> { ["derived"] = new OpenApiSchemaReference("Derived", document) },
        };
        var schema = new Mock<IOpenApiSchema>();
        schema.SetupGet(static x => x.Type).Returns(JsonSchemaType.Object);
        schema.SetupGet(static x => x.Properties).Returns(new Dictionary<string, IOpenApiSchema>
        {
            ["kind"] = new OpenApiSchema { Type = JsonSchemaType.String },
        });
        // Force the class-building path to reenter after publication, as opposed to the ordinary
        // reference lookup which immediately returns the published class. Keep the API private.
        var addModelClass = typeof(KiotaBuilder).GetMethod("AddModelClass", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(addModelClass);
        CodeClass BuildClass() => Assert.IsType<CodeClass>(addModelClass.Invoke(builder, [node, schema.Object, "Model", models, null, null]));
        CodeClass reenteredClass = null;
        schema.SetupGet(static x => x.Discriminator).Returns(() =>
        {
            if (reenteredClass is null)
            {
                // Disable the callback before invoking the nested build so a broken guard fails
                // an assertion rather than overflowing the test process's stack.
                schema.SetupGet(static x => x.Discriminator).Returns(discriminator);
                reenteredClass = BuildClass();
                Assert.DoesNotContain(reenteredClass.Methods, static x => x.IsOfKind(CodeMethodKind.Factory));
            }
            return discriminator;
        });
        var model = BuildClass();
        Assert.Same(model, reenteredClass);
        AssertModelFactory(model);
        Assert.Equal("kind", model.DiscriminatorInformation.DiscriminatorPropertyName);
        var mapping = Assert.Single(model.DiscriminatorInformation.DiscriminatorMappings);
        Assert.Equal("derived", mapping.Key);
        var derived = Assert.IsType<CodeClass>(Assert.IsType<CodeType>(mapping.Value).TypeDefinition);
        Assert.Same(model, derived.BaseClass);
        AssertModelFactory(derived);
    }

    private static void AssertModelFactory(CodeClass model)
    {
        var factory = Assert.Single(model.Methods, static x => x.IsOfKind(CodeMethodKind.Factory));
        Assert.Equal("CreateFromDiscriminatorValue", factory.Name);
        Assert.Same(model, Assert.IsType<CodeType>(factory.ReturnType).TypeDefinition);
        Assert.Single(model.Methods, static x => x.Name.StartsWith("CreateFromDiscriminatorValue", StringComparison.Ordinal));
    }
}
