using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder;
using Kiota.Builder.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kiota {
    public class KiotaHost {
        public RootCommand GetRootCommand()
        {
            var outputOption = new Option<string>("--output", () => "./output", "The output directory path for the generated code files.");
            outputOption.AddAlias("-o");
            
            var languageOption = new Option<GenerationLanguage>("--language", () => GenerationLanguage.CSharp, "The target language for the generated code files.");
            languageOption.AddAlias("-l");
            AddEnumValidator(languageOption, "language");
            var classOption = new Option<string>("--class-name", () => "ApiClient", "The class name to use for the core client class.");
            classOption.AddAlias("-c");
            AddStringRegexValidator(classOption, @"^[a-zA-Z_][\w_-]+", "class name");

            var namespaceOption = new Option<string>("--namespace-name", () => "ApiSdk", "The namespace to use for the core client class specified with the --class-name option.");
            namespaceOption.AddAlias("-n");
            AddStringRegexValidator(namespaceOption, @"^[\w][\w\._-]+", "namespace name");

            var logLevelOption = new Option<LogLevel>("--loglevel", () => LogLevel.Warning, "The log level to use when logging messages to the main output.");
            logLevelOption.AddAlias("--ll");
            AddEnumValidator(logLevelOption, "log level");
            var descriptionOption = new Option<string>("--openapi", () => "openapi.yml", "The path to the OpenAPI description file used to generate the code files.");
            descriptionOption.AddAlias("-d");

            var backingStoreOption = new Option<bool>("--backing-store", () => false, "Enables backing store for models.");
            backingStoreOption.AddAlias("-b");

            var serializerOption = new Option<List<string>>("--serializer", () => new List<string> {"Microsoft.Kiota.Serialization.Json.JsonSerializationWriterFactory"}, "The fully qualified class names for serializers. Accepts multiple values.");
            serializerOption.AddAlias("-s");

            var deserializerOption = new Option<List<string>>("--deserializer", () => new List<string> {"Microsoft.Kiota.Serialization.Json.JsonParseNodeFactory"}, "The fully qualified class names for deserializers. Accepts multiple values.");
            deserializerOption.AddAlias("--ds");

            var command = new RootCommand {
                outputOption,
                languageOption,
                descriptionOption,
                backingStoreOption,
                classOption,
                logLevelOption,
                namespaceOption,
                serializerOption,
                deserializerOption,
            };
            command.SetHandler<string, GenerationLanguage, string, bool, string, LogLevel, string, List<string>, List<string>, CancellationToken>(HandleCommandCall, outputOption, languageOption, descriptionOption, backingStoreOption, classOption, logLevelOption, namespaceOption, serializerOption, deserializerOption);
            return command;
        }
        private void AssignIfNotNullOrEmpty(string input, Action<GenerationConfiguration, string> assignment) {
            if (!string.IsNullOrEmpty(input))
                assignment.Invoke(Configuration, input);
        }
        private async Task<int> HandleCommandCall(string output, GenerationLanguage language, string openapi, bool backingstore, string classname, LogLevel loglevel, string namespacename, List<string> serializer, List<string> deserializer, CancellationToken cancellationToken) {
            AssignIfNotNullOrEmpty(output, (c, s) => c.OutputPath = s);
            AssignIfNotNullOrEmpty(openapi, (c, s) => c.OpenAPIFilePath = s);
            AssignIfNotNullOrEmpty(classname, (c, s) => c.ClientClassName = s);
            AssignIfNotNullOrEmpty(namespacename, (c, s) => c.ClientNamespaceName = s);
            Configuration.UsesBackingStore = backingstore;
            Configuration.Language = language;
            if(serializer?.Any() ?? false)
                Configuration.Serializers.AddRange(serializer.Select(x => x.TrimQuotes()));
            if(deserializer?.Any() ?? false)
                Configuration.Deserializers.AddRange(deserializer.Select(x => x.TrimQuotes()));

            #if DEBUG
            loglevel = loglevel > LogLevel.Debug ? LogLevel.Debug : loglevel;
            #endif

            Configuration.OpenAPIFilePath = GetAbsolutePath(Configuration.OpenAPIFilePath);
            Configuration.OutputPath = GetAbsolutePath(Configuration.OutputPath);

            var logger = LoggerFactory.Create((builder) => {
                builder
                    .AddConsole()
#if DEBUG
                    .AddDebug()
#endif
                    .SetMinimumLevel(loglevel);
            }).CreateLogger<KiotaBuilder>();

            logger.LogTrace("configuration: {configuration}", JsonSerializer.Serialize(Configuration));

            try {
                await new KiotaBuilder(logger, Configuration).GenerateSDK(cancellationToken);
                return 0;
            } catch (Exception ex) {
#if DEBUG
                logger.LogCritical(ex, "error generating the SDK: {exceptionMessage}", ex.Message);
                throw; // so debug tools go straight to the source of the exception when attached
#else
                logger.LogCritical("error generating the SDK: {exceptionMessage}", ex.Message);
                return 1;
#endif
            }
        }
        private static void AddStringRegexValidator(Option<string> option, string pattern, string parameterName) {
            var validator = new Regex(pattern);
            option.AddValidator((input) => {
                var value = input.GetValueForOption(option);
                if(string.IsNullOrEmpty(value) ||
                    !validator.IsMatch(value))
                        input.ErrorMessage = $"{value} is not a valid {parameterName} for the client, the {parameterName} must conform to {pattern}";
            });
        }
        private static void AddEnumValidator<T>(Option<T> option, string parameterName) where T: struct, Enum {
            option.AddValidator((input) => {
                if(input.Tokens.Any() &&
                    !Enum.TryParse<T>(input.Tokens[0].Value, true, out var _)) {
                        var validOptionsList = Enum.GetValues<T>().Select(x => x.ToString()).Aggregate((x, y) => x + ", " + y);
                        input.ErrorMessage = $"{input.Tokens[0].Value} is not a supported generation {parameterName}, supported values are {validOptionsList}";
                    }
            });
        }
        private GenerationConfiguration Configuration { get => ConfigurationFactory.Value; }
        private readonly Lazy<GenerationConfiguration> ConfigurationFactory = new (() => {
            var builder = new ConfigurationBuilder();
            var configuration = builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables(prefix: "KIOTA_")
                    .Build();
            var configObject = new GenerationConfiguration();
            configuration.Bind(configObject);
            return configObject;
        });
        private static string GetAbsolutePath(string source) => Path.IsPathRooted(source) || source.StartsWith("http") ? source : Path.Combine(Directory.GetCurrentDirectory(), source);
    }
    
}
