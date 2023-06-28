using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging;

using Xunit;

namespace Kiota.Builder.IntegrationTests;
public class GenerateSample : IDisposable
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
            OpenAPIFilePath = "ToDoApi.yaml",
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
            OpenAPIFilePath = "ModelWithDictionary.yaml",
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
            OpenAPIFilePath = "ResponseWithMultipleReturnFormats.yaml",
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
            OpenAPIFilePath = "InheritingErrors.yaml",
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
            OpenAPIFilePath = "NoUnderscoresInModel.yaml",
            OutputPath = OutputPath,
            CleanOutput = true,
        };
        await new KiotaBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());

        Assert.Empty(Directory.GetFiles(OutputPath, "*_*", SearchOption.AllDirectories));
    }
    [InlineData(GenerationLanguage.Python)]
    [InlineData(GenerationLanguage.Ruby)]
    [Theory]
    public async Task GeneratesQueryParametersMapper(GenerationLanguage language)
    {
        var logger = LoggerFactory.Create(builder =>
        {
        }).CreateLogger<KiotaBuilder>();

        var OutputPath = $".\\Generated\\GeneratesQueryParametersMapper\\{language}";
        var configuration = new GenerationConfiguration
        {
            Language = language,
            OpenAPIFilePath = "GeneratesQueryMappers.yaml",
            OutputPath = OutputPath,
            CleanOutput = true,
        };
        await new KiotaBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());

        var fullText = "";
        foreach (var file in Directory.GetFiles(OutputPath, "*.*", SearchOption.AllDirectories))
        {
            fullText += File.ReadAllText(file);
        }

        Assert.Contains("get_query_parameter", fullText);
    }
}
