using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
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
            languageOption.Argument.AddValidator((input) => {
                if(input.Tokens.Any() &&
                    !Enum.TryParse<GenerationLanguage>(input.Tokens.First().Value, true, out var _)) {
                        var languagesList = Enum.GetValues<GenerationLanguage>().Select(x => x.ToString()).Aggregate((x, y) => x + ", " + y);
                        var message = $"{input.Tokens.First().Value} is not a supported generation language, supported values are {languagesList}";
                        return message;
                    }
                else
                    return null;
            });
            const string classNameValidationPattern = @"^[a-zA-Z_][\w_-]+";
            var classNameValidator = new Regex(classNameValidationPattern);
            var classOption = new Option("--class-name", "The class name to use the for main entry point") { Argument = new Argument<string>(() => "GraphClient") };
            classOption.AddAlias("-c");
            classOption.Argument.AddValidator((input) => {
                if(input.Tokens.Any() &&
                    !classNameValidator.IsMatch(input.Tokens.First().Value))
                        return $"{input.Tokens.First().Value} is not a valid class name for the client, the class name must conform to {classNameValidationPattern}";
                else
                    return null;
            });

            const string namespaceNameValidationPattern = @"^[\w][\w\._-]+";
            var namespaceNameValidator = new Regex(namespaceNameValidationPattern);
            var namespaceOption = new Option("--namespace-name", "The namespace name to use the for main entry point") { Argument = new Argument<string>(() => "GraphClient") };
            namespaceOption.AddAlias("-n");
            namespaceOption.Argument.AddValidator((input) => {
                if(input.Tokens.Any() &&
                    !namespaceNameValidator.IsMatch(input.Tokens.First().Value))
                        return $"{input.Tokens.First().Value} is not a valid namespace name for the client, the namespace name must conform to {namespaceNameValidationPattern}";
                else
                    return null;
            });

            var logLevelOption = new Option("--loglevel", "The log level to use when logging events to the main output.") { Argument = new Argument<LogLevel>(() => LogLevel.Warning)};
            logLevelOption.AddAlias("--ll");
            logLevelOption.Argument.AddValidator((input) => {
                if(input.Tokens.Any() &&
                    !Enum.TryParse<LogLevel>(input.Tokens.First().Value, true, out var _)) {
                        var logLevelsList = Enum.GetValues<LogLevel>().Select(x => x.ToString()).Aggregate((x, y) => x + ", " + y);
                        var message = $"{input.Tokens.First().Value} is not a supported generation log level, supported values are {logLevelsList}";
                        return message;
                    }
                else
                    return null;
            });
            var descriptionOption = new Option("--openapi", "The path to the OpenAPI description file used to generate the code.") {Argument = new Argument<string>(() => "openapi.yml")};
            descriptionOption.AddAlias("-d");

            var command = new RootCommand {
                outputOption,
                languageOption,
                descriptionOption,
                classOption,
                logLevelOption,
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

                #if DEBUG
                loglevel = loglevel > LogLevel.Debug ? LogLevel.Debug : loglevel;
                #endif

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
