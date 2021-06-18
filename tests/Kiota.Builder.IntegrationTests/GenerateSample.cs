using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Kiota.Builder.integrationtests
{
    public class GenerateSample
    {
        [InlineData(GenerationLanguage.CSharp, null)]
        [InlineData(GenerationLanguage.Java, null)]
        [InlineData(GenerationLanguage.TypeScript, null)]
        [InlineData(GenerationLanguage.CSharp, "Microsoft.Kiota.Abstractions.Store.InMemoryBackingStore")]
        [InlineData(GenerationLanguage.Java, "com.microsoft.kiota.store.InMemoryBackingStore")]
        [InlineData(GenerationLanguage.TypeScript, "@microsoft/kiota-abstractions.InMemoryBackingStore")]
        [Theory]
        public async Task GeneratesTodo(GenerationLanguage language, string backingStore) {
            var logger = LoggerFactory.Create((builder) => {
            }).CreateLogger<KiotaBuilder>();

            var backingStoreSuffix = string.IsNullOrEmpty(backingStore) ? string.Empty : "BackingStore";
            var configuration = new GenerationConfiguration
            {
                Language = GenerationLanguage.CSharp,
                OpenAPIFilePath = "ToDoApi.yaml",
                OutputPath = $".\\Generated\\{language}{backingStoreSuffix}",
                BackingStore = backingStore,
            };
            await new KiotaBuilder(logger, configuration).GenerateSDK();
        }
    }
}
