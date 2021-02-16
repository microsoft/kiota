using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;

namespace kiota.core.integrationtests
{
    public class GenerateSample
    {

        [Fact]
        public async Task GenerateCSharpTodo() {
            var logger = LoggerFactory.Create((builder) => {
            }).CreateLogger<KiotaBuilder>();

            var configuration = new GenerationConfiguration
            {
                Language = GenerationLanguage.CSharp,
                OpenAPIFilePath = "ToDoApi.yaml",
                OutputPath = ".\\Generated\\CSharp"
            };
            await new KiotaBuilder(logger, configuration).GenerateSDK();
        }

        [Fact]
        public async Task GenerateJavaTodo()
        {
            var logger = LoggerFactory.Create((builder) => {
            }).CreateLogger<KiotaBuilder>();

            var configuration = new GenerationConfiguration
            {
                Language = GenerationLanguage.Java,
                OpenAPIFilePath = "ToDoApi.yaml",
                OutputPath = ".\\Generated\\Java"
            };
            await new KiotaBuilder(logger, configuration).GenerateSDK();
        }

        [Fact]
        public async Task GenerateTypeScriptTodo()
        {
            var logger = LoggerFactory.Create((builder) => {
            }).CreateLogger<KiotaBuilder>();

            var configuration = new GenerationConfiguration
            {
                Language = GenerationLanguage.TypeScript,
                OpenAPIFilePath = "ToDoApi.yaml",
                OutputPath = ".\\Generated\\TypeScript"
            };
            await new KiotaBuilder(logger, configuration).GenerateSDK();
        }
    }
}
