using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Kiota.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace kiota
{
    static class Program
    {
        static async Task<int> Main(string[] args)
        {
            var configuration = LoadDefaultConfiguration();
            var command = GetRootCommand(configuration);

            return await command.InvokeAsync(args);
        }

        private static RootCommand GetRootCommand(GenerationConfiguration configuration)
        {
            var outputOption = new Option("--output", "The ouput path of the folder the code will be generated in.") { Argument = new Argument<string>(() => "./output") };
            outputOption.AddAlias("-o");
            var languageOption = new Option("--language", "The language to generate the code in.") { Argument = new Argument<GenerationLanguage?>(() => GenerationLanguage.CSharp) };
            languageOption.AddAlias("-l");
            var classOption = new Option("--class-name", "The class name to use the for main entry point") { Argument = new Argument<string>(() => "GraphClient") };
            classOption.AddAlias("-c");
            var namespaceOption = new Option("--namespace-name", "The namespace name to use the for main entry point") { Argument = new Argument<string>(() => "GraphClient") };
            namespaceOption.AddAlias("-n");

            var command = new RootCommand {
                outputOption,
                languageOption,
                new Option("--openapi", "The path to the OpenAPI description file used to generate the code.") {Argument = new Argument<string>(() => "openapi.yml")},
                classOption,
                new Option("--loglevel") { Argument = new Argument<LogLevel>(() => LogLevel.Warning)},
                namespaceOption,
            };
            command.Handler = CommandHandler.Create<string, GenerationLanguage?, string, string, LogLevel, string>(async (output, language, openapi, classname, loglevel, namespacename) =>
            {
                if (!string.IsNullOrEmpty(output))
                    configuration.OutputPath = output;
                if (!string.IsNullOrEmpty(openapi))
                    configuration.OpenAPIFilePath = openapi;
                if (!string.IsNullOrEmpty(classname))
                    configuration.ClientClassName = classname;
                if (!string.IsNullOrEmpty(namespacename))
                    configuration.ClientNamespaceName = namespacename;
                if (language.HasValue)
                    configuration.Language = language.Value;

                configuration.OpenAPIFilePath = GetAbsolutePath(configuration.OpenAPIFilePath);
                configuration.OutputPath = GetAbsolutePath(configuration.OutputPath);

                var logger = LoggerFactory.Create((builder) => {
                    builder
                        .AddConsole()
                        .AddDebug()
                        .SetMinimumLevel(loglevel);
                }).CreateLogger<KiotaBuilder>();

                logger.LogTrace($"configuration: {JsonSerializer.Serialize(configuration)}");

                await new KiotaBuilder(logger, configuration).GenerateSDK();

            });
            return command;
        }

        private static GenerationConfiguration LoadDefaultConfiguration() {
            var builder = new ConfigurationBuilder();
            var configuration = builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables(prefix: "KIOTA_")
                    .Build();
            var configObject = new GenerationConfiguration();
            configuration.Bind(configObject);
            return configObject;
        }

        private static string GetAbsolutePath(string source) => Path.IsPathRooted(source) || source.StartsWith("http") ? source : Path.Combine(Directory.GetCurrentDirectory(), source);
    }
}
