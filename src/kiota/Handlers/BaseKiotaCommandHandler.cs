using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Kiota.Builder;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers;

internal abstract class BaseKiotaCommandHandler : ICommandHandler
{
    internal static readonly HttpClient httpClient = new();
    public Option<LogLevel> LogLevelOption { get;set; }
    protected KiotaConfiguration Configuration { get => ConfigurationFactory.Value; }
    private readonly Lazy<KiotaConfiguration> ConfigurationFactory = new (() => {
        var builder = new ConfigurationBuilder();
        var configuration = builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables(prefix: "KIOTA_")
                .Build();
        var configObject = new KiotaConfiguration();
        configuration.Bind(configObject);
        return configObject;
    });
    public int Invoke(InvocationContext context)
    {
        return InvokeAsync(context).GetAwaiter().GetResult();
    }
    public abstract Task<int> InvokeAsync(InvocationContext context);
    protected (ILoggerFactory, ILogger<T>) GetLoggerAndFactory<T>(InvocationContext context) {
        LogLevel logLevel = context.ParseResult.GetValueForOption(LogLevelOption);
#if DEBUG
        logLevel = logLevel > LogLevel.Debug ? LogLevel.Debug : logLevel;
#endif

        var loggerFactory = LoggerFactory.Create(builder => {
            builder
                .AddConsole()
#if DEBUG
                .AddDebug()
#endif
                .SetMinimumLevel(logLevel);
        });
        var logger = loggerFactory.CreateLogger<T>();
        return (loggerFactory, logger);
    }
    protected static string GetAbsolutePath(string source) {
        if (string.IsNullOrEmpty(source))
            return string.Empty;
        return Path.IsPathRooted(source) || source.StartsWith("http") ? source : NormalizeSlashesInPath(Path.Combine(Directory.GetCurrentDirectory(), source));
    }
    protected void AssignIfNotNullOrEmpty(string input, Action<GenerationConfiguration, string> assignment) {
        if (!string.IsNullOrEmpty(input))
            assignment.Invoke(Configuration.Generation, input);
    }
    protected static string NormalizeSlashesInPath(string path) {
        if (string.IsNullOrEmpty(path))
            return path;
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            return path.Replace('/', '\\');
        return path.Replace('\\', '/');
    }
    private readonly Lazy<bool> tutorialMode = new(() => {
        var kiotaInContainerRaw = Environment.GetEnvironmentVariable("KIOTA_TUTORIAL");
        if (!string.IsNullOrEmpty(kiotaInContainerRaw) && bool.TryParse(kiotaInContainerRaw, out var kiotaTutorial)) {
            return kiotaTutorial;
        }
        return true;
    });
    protected bool TutorialMode => tutorialMode.Value;
    private static void DisplayHint(params string[] messages) {
        Console.WriteLine();
        DisplayMessages(ConsoleColor.Blue, messages);
    }
    private static void DisplayMessages(ConsoleColor color, params string[] messages) {
        Console.ForegroundColor = color;
        foreach(var message in messages)
            Console.WriteLine(message);
        Console.ResetColor();
    }
    protected static void DisplayError(params string[] messages) {
        DisplayMessages(ConsoleColor.Red, messages);
    }
    protected static void DisplayWarning(params string[] messages) {
        DisplayMessages(ConsoleColor.Yellow, messages);
    }
    protected static void DisplaySuccess(params string[] messages) {
        DisplayMessages(ConsoleColor.Green, messages);
    }
    protected static void DisplayInfo(params string[] messages) {
        DisplayMessages(ConsoleColor.White, messages);
    }
    protected void DisplayDownloadHint(string searchTerm, string version) {
        if(TutorialMode) {
            var example = string.IsNullOrEmpty(version) ?
                $"Example: kiota download {searchTerm} -o <output path>" :
                $"Example: kiota download {searchTerm} -v {version} -o <output path>";
            DisplayHint("Hint: use kiota download to download the OpenAPI description.", example);
        }
    }
    protected void DisplayShowHint(string searchTerm, string version, string path = null) {
        if(TutorialMode) {
            var example = path switch {
                _ when !string.IsNullOrEmpty(path) => $"Example: kiota show -d {path}",
                _ when string.IsNullOrEmpty(version) => $"Example: kiota show -k {searchTerm}",
                _ => $"Example: kiota show -k {searchTerm} -v {version}",
            };
            DisplayHint("Hint: use kiota show to display a tree of paths present in the OpenAPI description.", example);
        }
    }
    protected void DisplayShowAdvancedHint(string searchTerm, string version, IEnumerable<string> includePaths, IEnumerable<string> excludePaths, string path = null) {
        if(TutorialMode && !includePaths.Any() && !excludePaths.Any()) {
            var example = path switch {
                _ when !string.IsNullOrEmpty(path) => $"Example: kiota show -d {path} --include-path **/foo",
                _ when string.IsNullOrEmpty(version) => $"Example: kiota show -k {searchTerm} --include-path **/foo",
                _ => $"Example: kiota show -k {searchTerm} -v {version} --include-path **/foo",
            };
            DisplayHint("Hint: use the --include-path and --exclude-path options with glob patterns to filter the paths displayed.", example);
        }
    }
    protected static void DisplaySearchAddHint() {
        DisplayHint("Hint: add your own API to the search result https://aka.ms/kiota/addapi.");
    }
    protected void DisplaySearchHint(string firstKey, string version) {
        if (TutorialMode &&!string.IsNullOrEmpty(firstKey)) {
            var example = string.IsNullOrEmpty(version) ?
                $"Example: kiota search {firstKey}" :
                $"Example: kiota search {firstKey} -v {version}";
            DisplayHint("Hint: multiple matches found, use the key as the search term to display the details of a specific description.", example);
        }
    }
    protected void DisplayGenerateHint(string path, IEnumerable<string> includedPaths, IEnumerable<string> excludedPaths) {
        if(TutorialMode) {
            var includedPathsSuffix = ((includedPaths?.Any() ?? false)? " -i " : string.Empty) + string.Join(" -i ", includedPaths);
            var excludedPathsSuffix = ((excludedPaths?.Any() ?? false)? " -e " : string.Empty) + string.Join(" -e ", excludedPaths);
            var example = $"Example: kiota generate -l <language> -o <output path> -d {path}{includedPathsSuffix}{excludedPathsSuffix}";
            DisplayHint("Hint: use kiota generate to generate a client for the OpenAPI description.", example);
        }
    }
    protected void DisplayGenerateAdvancedHint(IEnumerable<string> includePaths, IEnumerable<string> excludePaths, string path) {
        if(TutorialMode && !includePaths.Any() && !excludePaths.Any()) {
            DisplayHint("Hint: use the --include-path and --exclude-path options with glob patterns to filter the paths generated.",
                        $"Example: kiota generate --include-path **/foo -d {path}");
        }
    }
    protected void DisplayInfoHint(GenerationLanguage language, string path) {
        if(TutorialMode) {
            DisplayHint("Hint: use the info command to get the list of dependencies you need to add to your project.",
                        $"Example: kiota info -d {path} -l {language}");
        }
    }
    protected void DisplayCleanHint(string commandName) {
        if(TutorialMode) {
            DisplayHint("Hint: to force the generation to overwrite an existing client pass the --clean-output switch.",
                        $"Example: kiota {commandName} --clean-output");
        }
    }
    protected void DisplayInfoAdvancedHint() {
        if(TutorialMode) {
            DisplayHint("Hint: use the language argument to get the list of dependencies you need to add to your project.",
                        "Example: kiota info -l <language>");
        }
    }
}
