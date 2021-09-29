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
        [InlineData(GenerationLanguage.PHP, true)]
        [InlineData(GenerationLanguage.TypeScript, true)]
        [Theory]
        public async Task GeneratesTodo(GenerationLanguage language, bool backingStore) {
            var logger = LoggerFactory.Create((builder) => {
            }).CreateLogger<KiotaBuilder>();

            var backingStoreSuffix = backingStore ? string.Empty : "BackingStore";
            var configuration = new GenerationConfiguration
            {
                Language = GenerationLanguage.CSharp,
                OpenAPIFilePath = "ToDoApi.yaml",
                OutputPath = $".\\Generated\\{language}{backingStoreSuffix}",
                UsesBackingStore = backingStore,
            };
            await new KiotaBuilder(logger, configuration).GenerateSDK();
        }
    }
}
