using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Kiota.Builder.integrationtests
{
    public class GenerateSample
    {
        [InlineData(GenerationLanguage.CSharp, false)]
        [InlineData(GenerationLanguage.Java, false)]
        [InlineData(GenerationLanguage.TypeScript, false)]
        [InlineData(GenerationLanguage.Go, false)]
        [InlineData(GenerationLanguage.Ruby, false)]
        [InlineData(GenerationLanguage.CSharp, true)]
        [InlineData(GenerationLanguage.Java, true)]
        [InlineData(GenerationLanguage.TypeScript, true)]
        [Theory]
        public async Task GeneratesTodo(GenerationLanguage language, bool backingStore) {
            var logger = LoggerFactory.Create((builder) => {
            }).CreateLogger<KiotaBuilder>();

            var backingStoreSuffix = backingStore ? string.Empty : "BackingStore";
            var configuration = new GenerationConfiguration
            {
                Language = language,
                OpenAPIFilePath = "ToDoApi.yaml",
                OutputPath = $".\\Generated\\Todo\\{language}{backingStoreSuffix}",
                UsesBackingStore = backingStore,
            };
            await new KiotaBuilder(logger, configuration).GenerateSDK();
        }


        [InlineData(GenerationLanguage.CSharp, false)]
        [InlineData(GenerationLanguage.Java, false)]
        [InlineData(GenerationLanguage.TypeScript, false)]
        [InlineData(GenerationLanguage.Go, false)]
        [InlineData(GenerationLanguage.Ruby, false)]
        [InlineData(GenerationLanguage.CSharp, true)]
        [InlineData(GenerationLanguage.Java, true)]
        [InlineData(GenerationLanguage.TypeScript, true)]
        [Theory]
        public async Task GeneratesModelWithDictionary(GenerationLanguage language, bool backingStore)
        {
            var logger = LoggerFactory.Create((builder) => {
            }).CreateLogger<KiotaBuilder>();

            var backingStoreSuffix = backingStore ? "BackingStore" : string.Empty;
            var configuration = new GenerationConfiguration
            {
                Language = language,
                OpenAPIFilePath = "ModelWithDictionary.yaml",
                OutputPath = $".\\Generated\\ModelWithDictionary\\{language}{backingStoreSuffix}",
                UsesBackingStore = backingStore,
            };
            await new KiotaBuilder(logger, configuration).GenerateSDK();
        }



    }
}
