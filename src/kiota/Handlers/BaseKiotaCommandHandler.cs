using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers;

internal abstract class BaseKiotaCommandHandler : ICommandHandler
{
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
    protected static string GetAbsolutePath(string source) => Path.IsPathRooted(source) || (source?.StartsWith("http") ?? false) ? source : NormalizeSlashesInPath(Path.Combine(Directory.GetCurrentDirectory(), source));
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
    protected void DisplayDownloadHint(string searchTerm, string version) {
        if(TutorialMode) {
            Console.WriteLine();
            Console.WriteLine("Hint: use kiota download to download the OpenAPI description.");
            if(string.IsNullOrEmpty(version))
                Console.WriteLine($"Example: kiota download {searchTerm} -o <output path>");
            else
                Console.WriteLine($"Example: kiota download {searchTerm} -v {version} -o <output path>");
        }
    }
    protected void DisplayShowHint(string searchTerm, string version, string path = null) {
        if(TutorialMode) {
            Console.WriteLine();
            Console.WriteLine("Hint: use kiota show to display a tree of paths present in the OpenAPI description.");
            if(!string.IsNullOrEmpty(path))
                Console.WriteLine($"Example: kiota show -d {path}");
            else if(string.IsNullOrEmpty(version))
                Console.WriteLine($"Example: kiota show -k {searchTerm}");
            else
                Console.WriteLine($"Example: kiota show -k {searchTerm} -v {version}");
        }
    }
    protected void DisplayShowAdvancedHint(string searchTerm, string version, string path = null) {
        if(TutorialMode) {
            Console.WriteLine();
            Console.WriteLine("Hint: use the --include-path and --exclude-path options with glob patterns to filter the paths displayed.");
            if(!string.IsNullOrEmpty(path))
                Console.WriteLine($"Example: kiota show -d {path} --include-path **/foo");
            else if(string.IsNullOrEmpty(version))
                Console.WriteLine($"Example: kiota show -k {searchTerm} --include-path **/foo");
            else
                Console.WriteLine($"Example: kiota show -k {searchTerm} -v {version} --include-path **/foo");
        }
    }
    protected void DisplaySearchHint(string firstKey, string version) {
        if (TutorialMode)
            if(!string.IsNullOrEmpty(firstKey)) {
                Console.WriteLine();
                Console.WriteLine("Hint: multiple matches found, use the key as the search term to display the details of a specific description.");
                if(string.IsNullOrEmpty(version))
                    Console.WriteLine($"Example: kiota search {firstKey}");
                else
                    Console.WriteLine($"Example: kiota search {firstKey} -v {version}");
            }
    }
    protected void DisplayGenerateHint(string path, IEnumerable<string> includedPaths, IEnumerable<string> excludedPaths) {
        if(TutorialMode) {
            Console.WriteLine();
            Console.WriteLine("Hint: use kiota generate to generate a client for the OpenAPI description.");
            var includedPathsSuffix = ((includedPaths?.Any() ?? false)? " -i" : string.Empty) + string.Join(" ", includedPaths);
            var excludedPathsSuffix = ((excludedPaths?.Any() ?? false)? " -e" : string.Empty) + string.Join(" ", excludedPaths);
            Console.WriteLine($"Example: kiota generate -l <language> -o <output path> -d {path}{includedPathsSuffix}{excludedPathsSuffix}");
        }
    }
    protected void DisplayInfoHint(GenerationLanguage language, string path) {
        if(TutorialMode) {
            Console.WriteLine();
            Console.WriteLine("Hint: use the info command to get the list of dependencies you need to add to your project.");
            Console.WriteLine($"Example: kiota info -d {path} -l {language}");
        }
    }
}
