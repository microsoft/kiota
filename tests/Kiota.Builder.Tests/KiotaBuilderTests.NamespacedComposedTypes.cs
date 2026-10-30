using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace Kiota.Builder.Tests;

public sealed partial class KiotaBuilderTests
{
    [Theory]
    [InlineData(GenerationLanguage.TypeScript)]
    [InlineData(GenerationLanguage.Python)]
    public async Task UsesAliasesForSameNamedRuntimeTypesAsync(GenerationLanguage language)
    {
        const string description = """
        openapi: 3.1.0
        info: {title: Alias collisions, version: 1.0.0}
        paths:
          /pet:
            get:
              responses:
                '200':
                  description: ok
                  content:
                    application/json:
                      schema: {$ref: '#/components/schemas/com.example.a.Pet'}
        components:
          schemas:
            com.example.a.Pet:
              type: object
              properties:
                kind: {type: string}
                otherPet: {$ref: '#/components/schemas/com.example.b.Pet'}
                base: {$ref: '#/components/schemas/com.example.c.Base'}
            com.example.b.Pet:
              oneOf: [{$ref: '#/components/schemas/com.example.a.Pet'}, {$ref: '#/components/schemas/com.example.b.Fish'}]
              discriminator:
                propertyName: kind
                mapping: {pet: '#/components/schemas/com.example.a.Pet', fish: '#/components/schemas/com.example.b.Fish'}
            com.example.b.Fish:
              type: object
              properties: {kind: {type: string}, fins: {type: integer}}
            com.example.c.Base:
              type: object
              required: [kind]
              discriminator:
                propertyName: kind
                mapping: {a: '#/components/schemas/com.example.a.Child', b: '#/components/schemas/com.example.b.Child'}
              properties: {kind: {type: string}}
            com.example.a.Child:
              allOf:
                - {$ref: '#/components/schemas/com.example.c.Base'}
                - {type: object, properties: {aValue: {type: string}}}
            com.example.b.Child:
              allOf:
                - {$ref: '#/components/schemas/com.example.c.Base'}
                - {type: object, properties: {bValue: {type: string}}}
        """;
        var outputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(outputPath);
        try
        {
            await using var stream = await GetDocumentStreamAsync(description);
            var configuration = new GenerationConfiguration
            {
                ClientClassName = "Api",
                ClientNamespaceName = "api",
                Language = language,
                OutputPath = outputPath,
            };
            var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, configuration, _httpClient);
            var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(document);
            var model = builder.CreateSourceModel(builder.CreateUriSpace(document));
            await ILanguageRefiner.RefineAsync(configuration, model, cancellationToken: TestContext.Current.CancellationToken);
            await builder.CreateLanguageSourceFilesAsync(language, model, TestContext.Current.CancellationToken);

            if (language == GenerationLanguage.TypeScript)
            {
                var ownerCode = await File.ReadAllTextAsync(FindOutputFile(outputPath, "a", "index.ts"), TestContext.Current.CancellationToken);
                var serializerImport = Regex.Match(ownerCode, @"serializePet as (?<alias>\w+)");
                Assert.True(serializerImport.Success);
                Assert.Contains($"pet.otherPet, {serializerImport.Groups["alias"].Value});", ownerCode);
                Assert.DoesNotContain("pet.otherPet, serializePet);", ownerCode);
            }
            else
            {
                var unionCode = await File.ReadAllTextAsync(FindOutputFile(outputPath, "b", "pet.py"), TestContext.Current.CancellationToken);
                var petImport = Regex.Match(unionCode, @"from \.\.a\.pet import Pet as (?<alias>\w+)");
                Assert.True(petImport.Success);
                Assert.Contains("result = Pet()", unionCode);
                Assert.Contains($"result.pet = {petImport.Groups["alias"].Value}()", unionCode);
                Assert.DoesNotContain("result.pet = Pet()", unionCode);

                var baseCode = await File.ReadAllTextAsync(FindOutputFile(outputPath, "c", "base.py"), TestContext.Current.CancellationToken);
                var firstChildImport = Regex.Match(baseCode, @"from \.\.a\.child import Child as (?<alias>\w+)");
                var secondChildImport = Regex.Match(baseCode, @"from \.\.b\.child import Child as (?<alias>\w+)");
                Assert.True(firstChildImport.Success);
                Assert.True(secondChildImport.Success);
                Assert.Contains($"return {firstChildImport.Groups["alias"].Value}()", baseCode);
                Assert.Contains($"return {secondChildImport.Groups["alias"].Value}()", baseCode);
                Assert.DoesNotContain("return Child()", baseCode);
            }
        }
        finally
        {
            Directory.Delete(outputPath, true);
        }

        static string FindOutputFile(string rootPath, params string[] suffixSegments)
        {
            var suffix = Path.Combine(suffixSegments);
            return Assert.Single(Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories), x => x.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Theory]
    [InlineData(GenerationLanguage.TypeScript)]
    [InlineData(GenerationLanguage.Python)]
    public async Task KeepsSameNamedComposedTypesInTheirReferencedNamespacesAsync(GenerationLanguage language)
    {
        const string description = """
        openapi: 3.1.0
        info: {title: Alias, version: 1.0.0}
        paths:
          /owner:
            get:
              responses:
                '200':
                  description: ok
                  content:
                    application/json:
                      schema: {$ref: '#/components/schemas/com.example.a.Owner'}
        components:
          schemas:
            com.example.a.Owner:
              type: object
              properties:
                pet: {$ref: '#/components/schemas/com.example.a.Pet'}
                otherPet: {$ref: '#/components/schemas/com.example.b.Pet'}
            com.example.a.Pet:
              oneOf: [{$ref: '#/components/schemas/com.example.a.Cat'}, {$ref: '#/components/schemas/com.example.a.Dog'}]
              discriminator:
                propertyName: kind
                mapping: {cat: '#/components/schemas/com.example.a.Cat', dog: '#/components/schemas/com.example.a.Dog'}
            com.example.b.Pet:
              oneOf: [{$ref: '#/components/schemas/com.example.b.Fish'}, {$ref: '#/components/schemas/com.example.b.Bird'}]
              discriminator:
                propertyName: kind
                mapping: {fish: '#/components/schemas/com.example.b.Fish', bird: '#/components/schemas/com.example.b.Bird'}
            com.example.a.Cat: {type: object, properties: {kind: {type: string}, lives: {type: integer}}}
            com.example.a.Dog: {type: object, properties: {kind: {type: string}, barks: {type: boolean}}}
            com.example.b.Fish: {type: object, properties: {kind: {type: string}, fins: {type: integer}}}
            com.example.b.Bird: {type: object, properties: {kind: {type: string}, wings: {type: integer}}}
        """;
        await using var stream = await GetDocumentStreamAsync(description);
        var configuration = new GenerationConfiguration
        {
            ClientClassName = "Api",
            ClientNamespaceName = "api",
            Language = language,
        };
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, configuration, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(document);
        var model = builder.CreateSourceModel(builder.CreateUriSpace(document));

        var sourceNamespaces = GetNamespaces(model).ToArray();
        var sourceOwnerNamespace = Assert.Single(sourceNamespaces, static x => x.Name.EndsWith(".models.com.example.a", StringComparison.OrdinalIgnoreCase));
        var sourceOwner = Assert.Single(sourceOwnerNamespace.Classes, static x => x.Name.Equals("Owner", StringComparison.OrdinalIgnoreCase));
        var sourcePet = Assert.IsType<CodeUnionType>(Assert.Single(sourceOwner.Properties, static x => x.Name.Equals("pet", StringComparison.OrdinalIgnoreCase)).Type);
        var sourceOtherPet = Assert.IsType<CodeUnionType>(Assert.Single(sourceOwner.Properties, static x => x.Name.Equals("otherPet", StringComparison.OrdinalIgnoreCase)).Type);
        Assert.Equal("api.models.com.example.a", sourcePet.TargetNamespace?.Name, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("api.models.com.example.b", sourceOtherPet.TargetNamespace?.Name, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(new[] { "Cat", "Dog" }, sourcePet.Types.Select(static x => x.Name).Order(StringComparer.OrdinalIgnoreCase));
        Assert.Equal(new[] { "Bird", "Fish" }, sourceOtherPet.Types.Select(static x => x.Name).Order(StringComparer.OrdinalIgnoreCase));

        await ILanguageRefiner.RefineAsync(configuration, model, cancellationToken: TestContext.Current.CancellationToken);

        var namespaces = GetNamespaces(model).ToArray();
        var firstNamespace = Assert.Single(namespaces, static x => x.Name.EndsWith(".models.com.example.a", StringComparison.OrdinalIgnoreCase));
        var secondNamespace = Assert.Single(namespaces, static x => x.Name.EndsWith(".models.com.example.b", StringComparison.OrdinalIgnoreCase));
        var firstPet = GetPetUnion(firstNamespace, language);
        var secondPet = GetPetUnion(secondNamespace, language);
        Assert.Equal(new[] { "Cat", "Dog" }, firstPet.Types.Select(static x => x.Name).Order(StringComparer.OrdinalIgnoreCase));
        Assert.Equal(new[] { "Bird", "Fish" }, secondPet.Types.Select(static x => x.Name).Order(StringComparer.OrdinalIgnoreCase));

        CodeElement owner = language == GenerationLanguage.TypeScript
            ? firstNamespace.FindChildByName<CodeInterface>("Owner")
            : firstNamespace.FindChildByName<CodeClass>("Owner", false);
        Assert.NotNull(owner);
        var properties = owner switch
        {
            CodeClass codeClass => codeClass.Properties,
            CodeInterface codeInterface => codeInterface.Properties,
            _ => throw new InvalidOperationException(),
        };
        var petProperty = Assert.Single(properties, static x => x.WireName.Equals("pet", StringComparison.OrdinalIgnoreCase));
        var otherPetProperty = Assert.Single(properties, static x => x.WireName.Equals("otherPet", StringComparison.OrdinalIgnoreCase));
        Assert.NotSame(Assert.IsType<CodeType>(petProperty.Type).TypeDefinition, Assert.IsType<CodeType>(otherPetProperty.Type).TypeDefinition);
        if (language == GenerationLanguage.TypeScript)
        {
            var ownerFile = Assert.IsType<CodeFile>(owner.Parent);
            var ownerSerializer = Assert.Single(ownerFile.GetChildElements(true).OfType<CodeFunction>(), static x => x.Name.Equals("serializeOwner", StringComparison.OrdinalIgnoreCase));
            var secondPetSerializer = Assert.IsType<CodeFunction>(secondNamespace.FindChildByName<CodeFunction>("serializePet"));
            var serializerUsing = Assert.Single(ownerSerializer.Usings, x => x.Declaration?.TypeDefinition == secondPetSerializer);
            Assert.False(string.IsNullOrEmpty(serializerUsing.Alias));

            var ownerDeserializer = Assert.Single(ownerFile.GetChildElements(true).OfType<CodeFunction>(), static x => x.Name.Equals("deserializeIntoOwner", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(ownerDeserializer.Usings, static x => x.Declaration?.Name.Equals("createBirdFromDiscriminatorValue", StringComparison.OrdinalIgnoreCase) == true);
            Assert.Contains(ownerDeserializer.Usings, static x => x.Declaration?.Name.Equals("createFishFromDiscriminatorValue", StringComparison.OrdinalIgnoreCase) == true);
        }
        else
        {
            var petUsings = Assert.IsType<CodeClass>(owner).Usings
                .Where(x => x.Declaration?.TypeDefinition == Assert.IsType<CodeType>(petProperty.Type).TypeDefinition ||
                            x.Declaration?.TypeDefinition == Assert.IsType<CodeType>(otherPetProperty.Type).TypeDefinition)
                .ToArray();
            Assert.Equal(2, petUsings.Length);
            Assert.All(petUsings, static x => Assert.False(string.IsNullOrEmpty(x.Alias)));
            Assert.Equal(2, petUsings.Select(static x => x.Alias).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }

        static CodeComposedTypeBase GetPetUnion(CodeNamespace targetNamespace, GenerationLanguage targetLanguage)
        {
            if (targetLanguage == GenerationLanguage.TypeScript)
            {
                var union = targetNamespace.FindChildByName<CodeUnionType>("Pet");
                Assert.NotNull(union);
                return union;
            }
            var wrapper = Assert.Single(targetNamespace.Classes, static x => x.OriginalComposedType is CodeUnionType && x.Name.Equals("Pet", StringComparison.OrdinalIgnoreCase));
            return wrapper.OriginalComposedType!;
        }

        static IEnumerable<CodeNamespace> GetNamespaces(CodeNamespace rootNamespace)
        {
            foreach (var childNamespace in rootNamespace.Namespaces)
            {
                yield return childNamespace;
                foreach (var descendant in GetNamespaces(childNamespace))
                    yield return descendant;
            }
        }
    }
}
