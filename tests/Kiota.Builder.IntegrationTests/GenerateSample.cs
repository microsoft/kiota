using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging;

using Xunit;

namespace Kiota.Builder.IntegrationTests;

public sealed class GenerateSample : IDisposable
{
    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
    private readonly HttpClient _httpClient = new();
    [InlineData(GenerationLanguage.CSharp, false)]
    [InlineData(GenerationLanguage.Java, false)]
    [InlineData(GenerationLanguage.TypeScript, false)]
    [InlineData(GenerationLanguage.Go, false)]
    [InlineData(GenerationLanguage.Dart, false)]
    [InlineData(GenerationLanguage.Ruby, false)]
    [InlineData(GenerationLanguage.CSharp, true)]
    [InlineData(GenerationLanguage.Java, true)]
    [InlineData(GenerationLanguage.PHP, false)]
    [InlineData(GenerationLanguage.TypeScript, true)]
    [InlineData(GenerationLanguage.Dart, true)]
    [Theory]
    public async Task GeneratesTodoAsync(GenerationLanguage language, bool backingStore)
    {
        var logger = LoggerFactory.Create(builder =>
        {
        }).CreateLogger<KiotaBuilder>();

        var backingStoreSuffix = backingStore ? string.Empty : "BackingStore";
        var configuration = new GenerationConfiguration
        {
            Language = language,
            OpenAPIFilePath = GetAbsolutePath("ToDoApi.yaml"),
            OutputPath = $".\\Generated\\Todo\\{language}{backingStoreSuffix}",
            UsesBackingStore = backingStore,
            CleanOutput = true,
        };
        await new KiotaBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());
    }
    [InlineData(GenerationLanguage.CSharp, false)]
    [InlineData(GenerationLanguage.Java, false)]
    [InlineData(GenerationLanguage.TypeScript, false)]
    [InlineData(GenerationLanguage.Go, false)]
    [InlineData(GenerationLanguage.Dart, false)]
    [InlineData(GenerationLanguage.Ruby, false)]
    [InlineData(GenerationLanguage.CSharp, true)]
    [InlineData(GenerationLanguage.Java, true)]
    [InlineData(GenerationLanguage.PHP, false)]
    [InlineData(GenerationLanguage.TypeScript, true)]
    [InlineData(GenerationLanguage.Dart, true)]
    [Theory]
    public async Task GeneratesModelWithDictionaryAsync(GenerationLanguage language, bool backingStore)
    {
        var logger = LoggerFactory.Create(builder =>
        {
        }).CreateLogger<KiotaBuilder>();

        var backingStoreSuffix = backingStore ? "BackingStore" : string.Empty;
        var configuration = new GenerationConfiguration
        {
            Language = language,
            OpenAPIFilePath = GetAbsolutePath("ModelWithDictionary.yaml"),
            OutputPath = $".\\Generated\\ModelWithDictionary\\{language}{backingStoreSuffix}",
            UsesBackingStore = backingStore,
            CleanOutput = true,
        };
        await new KiotaBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());
    }
    [InlineData(GenerationLanguage.CSharp, false)]
    [InlineData(GenerationLanguage.Java, false)]
    [InlineData(GenerationLanguage.TypeScript, false)]
    [InlineData(GenerationLanguage.Go, false)]
    [InlineData(GenerationLanguage.Dart, false)]
    [InlineData(GenerationLanguage.Ruby, false)]
    [InlineData(GenerationLanguage.CSharp, true)]
    [InlineData(GenerationLanguage.Java, true)]
    [InlineData(GenerationLanguage.PHP, false)]
    [InlineData(GenerationLanguage.TypeScript, true)]
    [InlineData(GenerationLanguage.Dart, true)]
    [Theory]
    public async Task GeneratesResponseWithMultipleReturnFormatsAsync(GenerationLanguage language, bool backingStore)
    {
        var logger = LoggerFactory.Create(builder =>
        {
        }).CreateLogger<KiotaBuilder>();

        var backingStoreSuffix = backingStore ? "BackingStore" : string.Empty;
        var configuration = new GenerationConfiguration
        {
            Language = language,
            OpenAPIFilePath = GetAbsolutePath("ResponseWithMultipleReturnFormats.yaml"),
            OutputPath = $".\\Generated\\ResponseWithMultipleReturnFormats\\{language}{backingStoreSuffix}",
            UsesBackingStore = backingStore,
            CleanOutput = true,
        };
        await new KiotaBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());
    }
    [InlineData(GenerationLanguage.CSharp)]
    [InlineData(GenerationLanguage.Java)]
    [InlineData(GenerationLanguage.Go)]
    [InlineData(GenerationLanguage.Dart)]
    [InlineData(GenerationLanguage.Ruby)]
    [InlineData(GenerationLanguage.Python)]
    [InlineData(GenerationLanguage.TypeScript)]
    [InlineData(GenerationLanguage.PHP)]
    [Theory]
    public async Task GeneratesErrorsInliningParentsAsync(GenerationLanguage language)
    {
        var logger = LoggerFactory.Create(builder =>
        {
        }).CreateLogger<KiotaBuilder>();

        var configuration = new GenerationConfiguration
        {
            Language = language,
            OpenAPIFilePath = GetAbsolutePath("InheritingErrors.yaml"),
            OutputPath = $".\\Generated\\ErrorInlineParents\\{language}",
            CleanOutput = true,
        };
        await new KiotaBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());
    }
    [InlineData(GenerationLanguage.CSharp)]
    [InlineData(GenerationLanguage.Java)]
    [InlineData(GenerationLanguage.Go)]
    [InlineData(GenerationLanguage.Dart)]
    [InlineData(GenerationLanguage.Ruby)]
    [InlineData(GenerationLanguage.Python)]
    [InlineData(GenerationLanguage.TypeScript)]
    [InlineData(GenerationLanguage.PHP)]
    [Theory]
    public async Task GeneratesCorrectEnumsAsync(GenerationLanguage language)
    {
        var logger = LoggerFactory.Create(builder =>
        {
        }).CreateLogger<KiotaBuilder>();

        var configuration = new GenerationConfiguration
        {
            Language = language,
            OpenAPIFilePath = GetAbsolutePath("EnumHandling.yaml"),
            OutputPath = $".\\Generated\\EnumHandling\\{language}",
            CleanOutput = true,
        };
        await new KiotaBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());
    }
    [InlineData(GenerationLanguage.Java)]
    [Theory]
    public async Task GeneratesIdiomaticChildrenNamesAsync(GenerationLanguage language)
    {
        var logger = LoggerFactory.Create(builder =>
        {
        }).CreateLogger<KiotaBuilder>();

        var OutputPath = $".\\Generated\\NoUnderscoresInObjectNames\\{language}";
        var configuration = new GenerationConfiguration
        {
            Language = language,
            OpenAPIFilePath = GetAbsolutePath("NoUnderscoresInModel.yaml"),
            OutputPath = OutputPath,
            CleanOutput = true,
        };
        await new KiotaBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());

        var fullText = "";
        foreach (var file in Directory.GetFiles(OutputPath, "*.*", SearchOption.AllDirectories))
        {
            fullText += await File.ReadAllTextAsync(file, cancellationToken: TestContext.Current.CancellationToken);
        }

        Assert.Empty(Directory.GetFiles(OutputPath, "*_*", SearchOption.AllDirectories));
        Assert.DoesNotContain("_", fullText);
    }
    [InlineData(GenerationLanguage.CSharp)]
    [InlineData(GenerationLanguage.Go)]
    [InlineData(GenerationLanguage.Dart)]
    [InlineData(GenerationLanguage.Java)]
    [InlineData(GenerationLanguage.PHP)]
    [InlineData(GenerationLanguage.Python)]
    [InlineData(GenerationLanguage.Ruby)]
    // [InlineData(GenerationLanguage.TypeScript)] // TODO: the "getQueryParameter" is added to the interface V1RequestBuilderGetQueryParameters but is not getting written because removed by ReplaceRequestConfigurationsQueryParamsWithInterfaces in the refiner
    [Theory]
    public async Task GeneratesUritemplateHintsAsync(GenerationLanguage language)
    {
        var logger = LoggerFactory.Create(builder =>
        {
        }).CreateLogger<KiotaBuilder>();

        var OutputPath = $".\\Generated\\GeneratesUritemplateHints\\{language}";
        var configuration = new GenerationConfiguration
        {
            Language = language,
            OpenAPIFilePath = GetAbsolutePath("GeneratesUritemplateHints.yaml"),
            OutputPath = OutputPath,
            CleanOutput = true,
        };
        await new KiotaBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());

        var fullText = "";
        foreach (var file in Directory.GetFiles(OutputPath, "*.*", SearchOption.AllDirectories))
        {
            fullText += File.ReadAllText(file);
        }

        switch (language)
        {
            case GenerationLanguage.CSharp:
                Assert.Contains("[QueryParameter(\"startDateTime\")]", fullText);
                break;
            case GenerationLanguage.Dart:
                Assert.Contains("'EndDateTime' : endDateTime", fullText);
                break;
            case GenerationLanguage.Go:
                Assert.Contains("uriparametername:\\\"startDateTime\\\"", fullText);
                break;
            case GenerationLanguage.Java:
                Assert.Contains("allQueryParams.put(\"EndDateTime\", endDateTime)", fullText);
                break;
            case GenerationLanguage.PHP:
                Assert.Contains("@QueryParameter(\"EndDateTime\")", fullText);
                break;
            case GenerationLanguage.Python:
                Assert.Contains("get_query_parameter", fullText);
                Assert.Contains("if original_name == \"end_date_time\":", fullText);
                break;
            case GenerationLanguage.Ruby:
                Assert.Contains("get_query_parameter", fullText);
                Assert.Contains("when \"start_date_time\"", fullText);
                break;
            default:
                throw new Exception($"Please implement a test-case for {language}");

        }
    }
    private static string GetAbsolutePath(string relativePath) => Path.Combine(Directory.GetCurrentDirectory(), relativePath);

    [InlineData("recursive-category-tree.yaml", GenerationLanguage.CSharp)]
    [InlineData("recursive-category-tree.yaml", GenerationLanguage.Java)]
    [InlineData("recursive-category-tree.yaml", GenerationLanguage.TypeScript)]
    [InlineData("recursive-category-tree.yaml", GenerationLanguage.Go)]
    [InlineData("recursive-category-tree.yaml", GenerationLanguage.Python)]
    [InlineData("non-identifier-schema-key.yaml", GenerationLanguage.CSharp)]
    [InlineData("non-identifier-schema-key.yaml", GenerationLanguage.Java)]
    [InlineData("non-identifier-schema-key.yaml", GenerationLanguage.TypeScript)]
    [InlineData("non-identifier-schema-key.yaml", GenerationLanguage.Go)]
    [InlineData("non-identifier-schema-key.yaml", GenerationLanguage.Python)]
    [Theory]
    public async Task ResolvesRecursiveDynamicRefAsync(string fixture, GenerationLanguage language)
    {
        // $dynamicRef inside a recursive type resolves through dynamic scope
        // to the active extending type, not UntypedNode / unknown / object.
        // See https://github.com/aqeelat/openapi-dynamicref-adoption-tracker
        var logger = LoggerFactory.Create(builder => { }).CreateLogger<KiotaBuilder>();
        var configuration = new GenerationConfiguration
        {
            Language = language,
            OpenAPIFilePath = GetAbsolutePath(fixture),
            OutputPath = Path.Combine(".", "Generated", "RecursiveDynamicRef", language.ToString(), Path.GetFileNameWithoutExtension(fixture)),
            CleanOutput = true,
        };
        await new KiotaBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());

        var modelsDir = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "Generated", "RecursiveDynamicRef", language.ToString(), Path.GetFileNameWithoutExtension(fixture)));
        var searchOption = SearchOption.AllDirectories;
        var modelFiles = modelsDir
            .EnumerateFiles("*.cs", searchOption)
            .Concat(modelsDir.EnumerateFiles("*.java", searchOption))
            .Concat(modelsDir.EnumerateFiles("*.ts", searchOption))
            .Concat(modelsDir.EnumerateFiles("*.go", searchOption))
            .Concat(modelsDir.EnumerateFiles("*.py", searchOption))
            .ToList();
        // Concatenate every model file's text so the assertions work for both per-file languages
        // (C#/Java/Go/Python) and single-file languages (TypeScript merges everything into index.ts).
        var allModelText = string.Join("\n", modelFiles.Select(f => File.ReadAllText(f.FullName)));
        // Verify the recursive `children` property is actually typed as the recursive type,
        // not UntypedNode / object / unknown. Renderings differ per language.
        switch (language)
        {
            case GenerationLanguage.CSharp:
                Assert.Contains("List<global::ApiSdk.Models.LocalizedCategory>", allModelText, StringComparison.Ordinal);
                break;
            case GenerationLanguage.Java:
                Assert.Contains("java.util.List<LocalizedCategory>", allModelText, StringComparison.Ordinal);
                break;
            case GenerationLanguage.TypeScript:
                Assert.Contains("LocalizedCategory[]", allModelText, StringComparison.Ordinal);
                break;
            case GenerationLanguage.Go:
                // Go generates an interface (<Type>able) for each model and uses it for collection items.
                Assert.Contains("[]LocalizedCategoryable", allModelText, StringComparison.Ordinal);
                break;
            case GenerationLanguage.Python:
                Assert.Contains("list[LocalizedCategory]", allModelText, StringComparison.Ordinal);
                break;
            default:
                throw new Exception($"Please implement a test-case for {language}");
        }
        Assert.DoesNotContain("UntypedNode", allModelText, StringComparison.Ordinal);
    }
}
