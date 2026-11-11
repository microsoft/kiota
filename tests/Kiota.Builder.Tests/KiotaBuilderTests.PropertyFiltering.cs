using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;
using Xunit;

#nullable enable

namespace Kiota.Builder.Tests;

public sealed partial class KiotaBuilderTests
{
    private const string EmptyBasePropertyFilterDescription = """
        openapi: 3.0.3
        info:
          title: Empty base property filtering
          version: 1.0.0
        servers:
          - url: https://example.com
        paths:
          /derived:
            get:
              responses:
                '200':
                  description: OK
                  content:
                    application/json:
                      schema:
                        $ref: '#/components/schemas/Derived'
        components:
          schemas:
            Base:
              type: object
              properties:
                metadata:
                  type: string
            Derived:
              allOf:
                - $ref: '#/components/schemas/Base'
                - type: object
                  properties:
                    name:
                      type: string
        """;

    private const string PropertyFilterDescription = """
        openapi: 3.0.3
        info:
          title: Property filtering
          version: 1.0.0
        servers:
          - url: https://example.com
        paths:
          /users:
            get:
              responses:
                '200':
                  description: OK
                  content:
                    application/json:
                      schema:
                        $ref: '#/components/schemas/user'
          /me:
            get:
              responses:
                '200':
                  description: OK
                  content:
                    application/json:
                      schema:
                        $ref: '#/components/schemas/user'
        components:
          schemas:
            entity:
              type: object
              properties:
                id:
                  type: string
                '@odata.type':
                  type: string
              discriminator:
                propertyName: '@odata.type'
                mapping:
                  '#user': '#/components/schemas/user'
            user:
              allOf:
                - $ref: '#/components/schemas/entity'
                - type: object
                  required:
                    - id
                    - displayName
                  properties:
                    displayName:
                      type: string
                    mail:
                      type: string
                    profile:
                      $ref: '#/components/schemas/profile'
                    friends:
                      type: array
                      items:
                        $ref: '#/components/schemas/user'
            profile:
              type: object
              properties:
                title:
                  type: string
                color:
                  $ref: '#/components/schemas/color'
            color:
              type: string
              enum: [red, blue]
            unused:
              type: object
              properties:
                mail:
                  type: string
                label:
                  type: string
        """;

    private async Task<OpenApiDocument> ReadPropertyFilterDocumentAsync(string? description = null)
    {
        await using var stream = await GetDocumentStreamAsync(description ?? PropertyFilterDescription);
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, new GenerationConfiguration(), _httpClient);
        return Assert.IsType<OpenApiDocument>(await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken));
    }

    private static OpenApiSchema UserPropertyFragment(OpenApiDocument document) =>
        Assert.IsType<OpenApiSchema>(document.Components!.Schemas!["user"].AllOf![1]);

    [Fact]
    public async Task PropertyAllowlistIsLocalAndDenialsWinAsync()
    {
        var document = await ReadPropertyFilterDocumentAsync();
        new OpenApiPropertyFilter(NullLogger.Instance).Apply(document, new GenerationConfiguration
        {
            IncludeProperties = ["user#mail", "user#profile"],
            ExcludeProperties = ["user#profile"],
        });
        Assert.Equal(["displayName", "mail"], UserPropertyFragment(document).Properties!.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(["label", "mail"], document.Components!.Schemas!["unused"].Properties!.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(["@odata.type", "id"], document.Components.Schemas["entity"].Properties!.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(2, document.Paths.Count);
    }

    [Theory]
    [InlineData("user#displayName")]
    [InlineData("entity#id")]
    [InlineData("entity#@odata.type")]
    public async Task PropertyExclusionRejectsRequiredAndDiscriminatorFieldsBeforeMutationAsync(string selector)
    {
        var document = await ReadPropertyFilterDocumentAsync();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new OpenApiPropertyFilter(NullLogger.Instance).Apply(document, new GenerationConfiguration
            {
                IncludeProperties = ["user#mail"],
                ExcludeProperties = [selector],
            }));
        Assert.Contains("required or discriminator", exception.Message, StringComparison.Ordinal);
        Assert.Contains("profile", UserPropertyFragment(document).Properties!.Keys);
    }

    [Fact]
    public async Task AllowlistPreservesInheritedConstraintsAndEmitsDiagnosticsAsync()
    {
        var document = await ReadPropertyFilterDocumentAsync();
        var messages = new List<string>();
        var logger = new RecordingLogger(messages);
        new OpenApiPropertyFilter(logger).Apply(document, new GenerationConfiguration
        {
            IncludeProperties = ["entity#id", "user#mail", "unused#mail"],
        });
        Assert.Contains("@odata.type", document.Components!.Schemas!["entity"].Properties!.Keys);
        Assert.Contains(messages, x => x.Contains("Retaining required or discriminator property entity#@odata.type", StringComparison.Ordinal));
        Assert.Contains(messages, x => x.Contains("component unused not referenced", StringComparison.Ordinal));
        Assert.DoesNotContain(messages, x => x.Contains("component user not referenced", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("user", "Expected component#property")]
    [InlineData("#mail", "Expected component#property")]
    [InlineData("user#", "Expected component#property")]
    [InlineData("User#mail", "Unknown component")]
    [InlineData("user#Mail", "Unknown owned property")]
    [InlineData("user#m?il", "Unknown owned property")]
    [InlineData("u*#MAIL", "No owned properties match")]
    [InlineData("unknown*#mail", "No components match")]
    [InlineData("user#profile.title", "Unknown owned property")]
    [InlineData("user#id", "declaring component: entity")]
    public async Task InvalidPropertySelectorsFailWithoutMutationAsync(string selector, string diagnostic)
    {
        var document = await ReadPropertyFilterDocumentAsync();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new OpenApiPropertyFilter(NullLogger.Instance).Apply(document, new GenerationConfiguration
            {
                IncludeProperties = ["user#mail"],
                ExcludeProperties = [selector],
            }));
        Assert.Contains(diagnostic, exception.Message, StringComparison.Ordinal);
        Assert.Contains("profile", UserPropertyFragment(document).Properties!.Keys);
    }

    [Theory]
    [InlineData("allOf")]
    [InlineData("anyOf")]
    [InlineData("oneOf")]
    public async Task TrimsOwnedCompositionFragmentsWithoutChangingBranchesAsync(string composition)
    {
        var document = await ReadPropertyFilterDocumentAsync(PropertyFilterDescription.Replace("allOf:", $"{composition}:", StringComparison.Ordinal));
        new OpenApiPropertyFilter(NullLogger.Instance).Apply(document, new GenerationConfiguration { ExcludeProperties = ["user#profile"] });
        var user = document.Components!.Schemas!["user"];
        var branches = user.AllOf ?? user.AnyOf ?? user.OneOf!;
        Assert.Equal(2, branches.Count);
        Assert.IsType<OpenApiSchemaReference>(branches[0]);
        Assert.DoesNotContain("profile", branches[1].Properties!.Keys);
        Assert.Contains("mail", branches[1].Properties!.Keys);
        Assert.Equal(2, document.Components.Schemas["color"].Enum!.Count);
    }

    [Fact]
    public async Task NonStarPunctuationRemainsLiteralAsync()
    {
        var document = await ReadPropertyFilterDocumentAsync();
        var properties = UserPropertyFragment(document).Properties!;
        properties["?"] = new OpenApiSchema { Type = JsonSchemaType.String };
        properties["custom#name"] = new OpenApiSchema { Type = JsonSchemaType.String };
        properties["Mail"] = new OpenApiSchema { Type = JsonSchemaType.String };
        new OpenApiPropertyFilter(NullLogger.Instance).Apply(document, new GenerationConfiguration
        {
            IncludeProperties = ["user#?", "user#custom#name", "user#Mail"],
        });
        Assert.Equal(["?", "Mail", "custom#name", "displayName"], properties.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task ComponentAndPropertyStarsResolveToOwnedFieldsAsync()
    {
        var document = await ReadPropertyFilterDocumentAsync();
        new OpenApiPropertyFilter(NullLogger.Instance).Apply(document, new GenerationConfiguration
        {
            IncludeProperties = ["u*#ma*"],
        });
        Assert.Equal(["displayName", "mail"], UserPropertyFragment(document).Properties!.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(["mail"], document.Components!.Schemas!["unused"].Properties!.Keys);
        Assert.Equal(["@odata.type", "id"], document.Components.Schemas["entity"].Properties!.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task WildcardOnlyFiltersComponentsWithMatchingOwnedPropertiesAsync()
    {
        var document = await ReadPropertyFilterDocumentAsync();
        new OpenApiPropertyFilter(NullLogger.Instance).Apply(document, new GenerationConfiguration { IncludeProperties = ["*#mail"] });
        Assert.Equal(["displayName", "mail"], UserPropertyFragment(document).Properties!.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(2, document.Components!.Schemas!["profile"].Properties!.Count);
        Assert.Equal(["@odata.type", "id"], document.Components.Schemas["entity"].Properties!.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task PropertyStarCanRetainEveryFieldOrExcludeMatchingOrdinaryFieldsAsync()
    {
        var document = await ReadPropertyFilterDocumentAsync();
        new OpenApiPropertyFilter(NullLogger.Instance).Apply(document, new GenerationConfiguration
        {
            IncludeProperties = ["user#*"],
            ExcludeProperties = ["u*#pro*"],
        });
        Assert.Equal(["displayName", "friends", "mail"], UserPropertyFragment(document).Properties!.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(["label", "mail"], document.Components!.Schemas!["unused"].Properties!.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task WildcardDenialsWinOverWildcardInclusionsAsync()
    {
        var document = await ReadPropertyFilterDocumentAsync();
        new OpenApiPropertyFilter(NullLogger.Instance).Apply(document, new GenerationConfiguration
        {
            IncludeProperties = ["user#*a*"],
            ExcludeProperties = ["user#ma*"],
        });
        Assert.Equal(["displayName"], UserPropertyFragment(document).Properties!.Keys);
    }

    [Theory]
    [InlineData("user#display*")]
    [InlineData("entity#@odata.*")]
    [InlineData("*#id")]
    public async Task WildcardExclusionsCannotRemoveProtectedFieldsAsync(string selector)
    {
        var document = await ReadPropertyFilterDocumentAsync();
        var exception = Assert.Throws<InvalidOperationException>(() => new OpenApiPropertyFilter(NullLogger.Instance)
            .Apply(document, new GenerationConfiguration { IncludeProperties = ["user#mail"], ExcludeProperties = [selector] }));
        Assert.Contains("required or discriminator", exception.Message, StringComparison.Ordinal);
        Assert.Contains("profile", UserPropertyFragment(document).Properties!.Keys);
    }

    [Fact]
    public async Task StarsMatchSlashNamesAndEscapeRegexPunctuationAsync()
    {
        var document = await ReadPropertyFilterDocumentAsync();
        var properties = UserPropertyFragment(document).Properties!;
        properties["literal.name"] = new OpenApiSchema { Type = JsonSchemaType.String };
        properties["literalXname"] = new OpenApiSchema { Type = JsonSchemaType.String };
        properties["custom/name"] = new OpenApiSchema { Type = JsonSchemaType.String };
        new OpenApiPropertyFilter(NullLogger.Instance).Apply(document, new GenerationConfiguration { IncludeProperties = ["user#literal.*", "user#custom*"] });
        Assert.Equal(["custom/name", "displayName", "literal.name"], properties.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task RequiredConstraintsInOperationInlineSchemasProtectComponentFieldsAsync()
    {
        var document = await ReadPropertyFilterDocumentAsync();
        var response = document.Paths["/me"].Operations![System.Net.Http.HttpMethod.Get].Responses!["200"];
        response.Content!["application/json"] = new OpenApiMediaType
        {
            Schema = new OpenApiSchema
            {
                AllOf =
                [
                    new OpenApiSchemaReference("user", document),
                    new OpenApiSchema { Required = new HashSet<string>(StringComparer.Ordinal) { "mail" } },
                ],
            },
        };
        Assert.Throws<InvalidOperationException>(() => new OpenApiPropertyFilter(NullLogger.Instance)
            .Apply(document, new GenerationConfiguration { ExcludeProperties = ["user#mail"] }));
    }

    [Fact]
    public async Task DefaultPropertySelectionIsANoOpAsync()
    {
        var document = await ReadPropertyFilterDocumentAsync();
        new OpenApiPropertyFilter(NullLogger.Instance).Apply(document, new GenerationConfiguration());
        Assert.Equal(4, UserPropertyFragment(document).Properties!.Count);
        Assert.Equal(2, document.Components!.Schemas!["entity"].Properties!.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TrimmingLastBasePropertyPreservesInheritanceAsync(bool explicitObjectType)
    {
        var document = await ReadPropertyFilterDocumentAsync(EmptyBasePropertyFilterDescription);
        var baseSchema = Assert.IsType<OpenApiSchema>(document.Components!.Schemas!["Base"]);
        if (!explicitObjectType)
            baseSchema.Type = null;
        var configuration = new GenerationConfiguration { ExcludeProperties = ["Base#metadata"] };
        new OpenApiPropertyFilter(NullLogger.Instance).Apply(document, configuration);
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, configuration, _httpClient);
        builder.SetOpenApiDocument(document);
        var model = builder.CreateSourceModel(builder.CreateUriSpace(document));
        var baseModel = Assert.IsType<CodeClass>(model.FindChildByName<CodeClass>("Base", true));
        var derivedModel = Assert.IsType<CodeClass>(model.FindChildByName<CodeClass>("Derived", true));
        Assert.Same(baseModel, derivedModel.StartBlock.Inherits?.TypeDefinition);
        Assert.DoesNotContain(baseModel.Properties, static x => x.Kind == CodePropertyKind.Custom);
        Assert.Contains(derivedModel.Properties, static x => x.Kind == CodePropertyKind.Custom && x.Name == "name");
        Assert.Empty(baseSchema.Properties!);
    }

    [Theory]
    [InlineData(GenerationLanguage.CSharp, "cs")]
    [InlineData(GenerationLanguage.TypeScript, "ts")]
    public async Task GeneratesModelsWithTrimmedEmptyBaseAsync(GenerationLanguage language, string extension)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"kiota-empty-base-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "api.yaml");
            await File.WriteAllTextAsync(path, EmptyBasePropertyFilterDescription, TestContext.Current.CancellationToken);
            var output = Path.Combine(directory, "client");
            var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, new GenerationConfiguration
            {
                OpenAPIFilePath = path,
                OutputPath = output,
                Language = language,
                ExcludeProperties = ["Base#metadata"],
                NoWorkspace = true,
            }, _httpClient);
            Assert.True(await builder.GenerateClientAsync(TestContext.Current.CancellationToken));
            var files = Directory.GetFiles(output, $"*.{extension}", SearchOption.AllDirectories);
            Assert.NotEmpty(files);
            var source = string.Join(Environment.NewLine, await Task.WhenAll(files.Select(x => File.ReadAllTextAsync(x, TestContext.Current.CancellationToken))));
            Assert.DoesNotContain("\"metadata\"", source, StringComparison.Ordinal);
            Assert.Contains("\"name\"", source, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task PropertyFilteringKeepsModelsStillUsedByOtherOperationsAsync()
    {
        var document = await ReadPropertyFilterDocumentAsync();
        document.Paths["/me"].Operations![System.Net.Http.HttpMethod.Get].Responses!["200"]
            .Content!["application/json"] = new OpenApiMediaType { Schema = new OpenApiSchemaReference("profile", document) };
        new OpenApiPropertyFilter(NullLogger.Instance).Apply(document, new GenerationConfiguration { IncludeProperties = ["user#mail"] });
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, new GenerationConfiguration(), _httpClient);
        builder.SetOpenApiDocument(document);
        var source = builder.CreateSourceModel(builder.CreateUriSpace(document));
        Assert.NotNull(source.FindChildByName<CodeClass>("Profile", true));
        Assert.NotNull(source.FindChildByName<CodeEnum>("Color", true));
    }

    [Fact]
    public async Task PropertyFilteringIsNotAppliedToPathListingAsync()
    {
        var path = Path.GetTempFileName();
        _tempFiles.Add(path);
        await File.WriteAllTextAsync(path, PropertyFilterDescription, TestContext.Current.CancellationToken);
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, new GenerationConfiguration
        {
            OpenAPIFilePath = path,
            IncludeProperties = ["unknown#property"],
            NoWorkspace = true,
        }, _httpClient);
        var (tree, _) = await builder.GetUrlTreeNodeAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(tree);
        Assert.Contains("profile", UserPropertyFragment(builder.OpenApiDocument!).Properties!.Keys);
    }

    [Fact]
    public async Task PropertySelectionsRegenerateAndCanBeClearedWithoutChangingDescriptionAsync()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"kiota-property-state-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "api.yaml");
            await File.WriteAllTextAsync(path, PropertyFilterDescription, TestContext.Current.CancellationToken);
            var config = new GenerationConfiguration
            {
                OpenAPIFilePath = path,
                OutputPath = Path.Combine(directory, "output"),
                IncludeProperties = ["user#mail"],
                SkipGeneration = true,
            };
            Assert.False(await new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, config, _httpClient).GenerateClientAsync(TestContext.Current.CancellationToken));
            config.SkipGeneration = false;
            Assert.True(await new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, config, _httpClient).GenerateClientAsync(TestContext.Current.CancellationToken));
            Assert.False(await new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, config, _httpClient).GenerateClientAsync(TestContext.Current.CancellationToken));
            Assert.DoesNotContain(Directory.GetFiles(config.OutputPath, "*.cs", SearchOption.AllDirectories),
                x => Path.GetFileName(x).Equals("Profile.cs", StringComparison.Ordinal));
            config.IncludeProperties.Clear();
            config.CleanOutput = true;
            Assert.True(await new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, config, _httpClient).GenerateClientAsync(TestContext.Current.CancellationToken));
            Assert.Contains(Directory.GetFiles(config.OutputPath, "*.cs", SearchOption.AllDirectories),
                x => Path.GetFileName(x).Equals("Profile.cs", StringComparison.Ordinal));
            Assert.Equal(PropertyFilterDescription, await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(GenerationLanguage.CSharp, "cs")]
    [InlineData(GenerationLanguage.TypeScript, "ts")]
    public async Task PropertyFilteringReducesGeneratedModelsAndFieldsAsync(GenerationLanguage language, string extension)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"kiota-property-filter-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var description = Path.Combine(directory, "api.yaml");
            await File.WriteAllTextAsync(description, PropertyFilterDescription, TestContext.Current.CancellationToken);
            var baseline = Path.Combine(directory, "baseline");
            var filtered = Path.Combine(directory, "filtered");
            foreach (var (output, trim) in new[] { (baseline, false), (filtered, true) })
            {
                var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, new GenerationConfiguration
                {
                    OpenAPIFilePath = description,
                    OutputPath = output,
                    Language = language,
                    NoWorkspace = true,
                    IncludeProperties = trim ? ["user#mail"] : [],
                }, _httpClient);
                Assert.True(await builder.GenerateClientAsync(TestContext.Current.CancellationToken));
            }
            var baselineFiles = Directory.GetFiles(baseline, $"*.{extension}", SearchOption.AllDirectories);
            var filteredFiles = Directory.GetFiles(filtered, $"*.{extension}", SearchOption.AllDirectories);
            Assert.True(filteredFiles.Length <= baselineFiles.Length);
            Assert.True(filteredFiles.Sum(static x => new FileInfo(x).Length) < baselineFiles.Sum(static x => new FileInfo(x).Length));
            Assert.DoesNotContain(filteredFiles, x => Path.GetFileName(x).Equals($"profile.{extension}", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(filteredFiles, x => Path.GetFileName(x).Equals($"color.{extension}", StringComparison.OrdinalIgnoreCase));
            var source = string.Join(Environment.NewLine, await Task.WhenAll(filteredFiles.Select(x => File.ReadAllTextAsync(x, TestContext.Current.CancellationToken))));
            Assert.DoesNotContain("\"profile\"", source, StringComparison.Ordinal);
            Assert.DoesNotContain("serializeProfile", source, StringComparison.Ordinal);
            Assert.DoesNotContain("class Profile", source, StringComparison.Ordinal);
            Assert.Contains("\"displayName\"", source, StringComparison.Ordinal);
            Assert.Contains("\"mail\"", source, StringComparison.Ordinal);
            Assert.Contains("additionalData", source, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(PropertyFilterDescription, await File.ReadAllTextAsync(description, TestContext.Current.CancellationToken));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private sealed class RecordingLogger(List<string> messages) : Microsoft.Extensions.Logging.ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId,
            TState state, Exception? exception, Func<TState, Exception?, string> formatter) => messages.Add(formatter(state, exception));
    }
}
