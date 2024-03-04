using System;
using System.IO;
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
    [InlineData(GenerationLanguage.Ruby, false)]
    [InlineData(GenerationLanguage.CSharp, true)]
    [InlineData(GenerationLanguage.Java, true)]
    [InlineData(GenerationLanguage.PHP, false)]
    [InlineData(GenerationLanguage.TypeScript, true)]
    [Theory]
    public async Task GeneratesTodo(GenerationLanguage language, bool backingStore)
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
        };
        await new KiotaBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());
    }
    [InlineData(GenerationLanguage.CSharp, false)]
    [InlineData(GenerationLanguage.Java, false)]
    [InlineData(GenerationLanguage.TypeScript, false)]
    [InlineData(GenerationLanguage.Go, false)]
    [InlineData(GenerationLanguage.Ruby, false)]
    [InlineData(GenerationLanguage.CSharp, true)]
    [InlineData(GenerationLanguage.Java, true)]
    [InlineData(GenerationLanguage.PHP, false)]
    [InlineData(GenerationLanguage.TypeScript, true)]
    [Theory]
    public async Task GeneratesModelWithDictionary(GenerationLanguage language, bool backingStore)
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
        };
        await new KiotaBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());
    }
    [InlineData(GenerationLanguage.CSharp, false)]
    [InlineData(GenerationLanguage.Java, false)]
    [InlineData(GenerationLanguage.TypeScript, false)]
    [InlineData(GenerationLanguage.Go, false)]
    [InlineData(GenerationLanguage.Ruby, false)]
    [InlineData(GenerationLanguage.CSharp, true)]
    [InlineData(GenerationLanguage.Java, true)]
    [InlineData(GenerationLanguage.PHP, false)]
    [InlineData(GenerationLanguage.TypeScript, true)]
    [Theory]
    public async Task GeneratesResponseWithMultipleReturnFormats(GenerationLanguage language, bool backingStore)
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
        };
        await new KiotaBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());
    }
    [InlineData(GenerationLanguage.CSharp)]
    [InlineData(GenerationLanguage.Java)]
    [InlineData(GenerationLanguage.Go)]
    [InlineData(GenerationLanguage.Ruby)]
    [InlineData(GenerationLanguage.Python)]
    [InlineData(GenerationLanguage.TypeScript)]
    [InlineData(GenerationLanguage.PHP)]
    [Theory]
    public async Task GeneratesErrorsInliningParents(GenerationLanguage language)
    {
        var logger = LoggerFactory.Create(builder =>
        {
        }).CreateLogger<KiotaBuilder>();

        var configuration = new GenerationConfiguration
        {
            Language = language,
            OpenAPIFilePath = GetAbsolutePath("InheritingErrors.yaml"),
            OutputPath = $".\\Generated\\ErrorInlineParents\\{language}",
        };
        await new KiotaBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());
    }
    [InlineData(GenerationLanguage.Java)]
    [Theory]
    public async Task GeneratesIdiomaticChildrenNames(GenerationLanguage language)
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
            fullText += File.ReadAllText(file);
        }

        Assert.Empty(Directory.GetFiles(OutputPath, "*_*", SearchOption.AllDirectories));
        Assert.DoesNotContain("_", fullText);
    }
    [InlineData(GenerationLanguage.CSharp)]
    [InlineData(GenerationLanguage.Go)]
    [InlineData(GenerationLanguage.Java)]
    [InlineData(GenerationLanguage.PHP)]
    [InlineData(GenerationLanguage.Python)]
    [InlineData(GenerationLanguage.Ruby)]
    // [InlineData(GenerationLanguage.TypeScript)] // TODO: the "getQueryParameter" is added to the interface V1RequestBuilderGetQueryParameters but is not getting written because removed by ReplaceRequestConfigurationsQueryParamsWithInterfaces in the refiner
    // [InlineData(GenerationLanguage.Swift)] // TODO: incomplete
    [Theory]
    public async Task GeneratesUritemplateHints(GenerationLanguage language)
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
            case GenerationLanguage.Go:
                Assert.Contains("`uriparametername:\"startDateTime\"`", fullText);
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
}
