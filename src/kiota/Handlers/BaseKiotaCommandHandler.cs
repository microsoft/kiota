using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using kiota.Authentication.GitHub.DeviceCode;
using Kiota.Builder;
using Kiota.Builder.Configuration;
using Kiota.Builder.SearchProviders.GitHub.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions.Authentication;

namespace kiota.Handlers;

internal abstract class BaseKiotaCommandHandler : ICommandHandler
{
    protected TempFolderCachingAccessTokenProvider GetGitHubDeviceStorageService(ILogger logger) => new()
    {
        Logger = logger,
        ApiBaseUrl = Configuration.Search.GitHub.ApiBaseUrl,
        Concrete = null,
        AppId = Configuration.Search.GitHub.AppId,
    };
    protected static TempFolderTokenStorageService GetGitHubPatStorageService(ILogger logger) => new()
    {
        Logger = logger,
        FileName = "pat-api.github.com"
    };
    internal static readonly HttpClient httpClient = new();
    public required Option<LogLevel> LogLevelOption
    {
        get; init;
    }
    protected KiotaConfiguration Configuration
    {
        get => ConfigurationFactory.Value;
    }
    private readonly Lazy<KiotaConfiguration> ConfigurationFactory = new(() =>
    {
        var builder = new ConfigurationBuilder();
        using var defaultStream = new MemoryStream(Kiota.Generated.KiotaAppSettings.Default());
        var configuration = builder.AddJsonStream(defaultStream)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables(prefix: "KIOTA_")
                .Build();
        var configObject = new KiotaConfiguration();
        configObject.BindConfiguration(configuration);
        return configObject;
    });
    private const string GitHubScope = "repo";
    private Func<CancellationToken, Task<bool>> GetIsGitHubDeviceSignedInCallback(ILogger logger) => (cancellationToken) =>
    {
        var provider = GetGitHubDeviceStorageService(logger);
        return provider.TokenStorageService.Value.IsTokenPresentAsync(cancellationToken);
    };
    private static Func<CancellationToken, Task<bool>> GetIsGitHubPatSignedInCallback(ILogger logger) => (cancellationToken) =>
    {
        var provider = GetGitHubPatStorageService(logger);
        return provider.IsTokenPresentAsync(cancellationToken);
    };
    private IAuthenticationProvider GetGitHubAuthenticationProvider(ILogger logger) =>
        new DeviceCodeAuthenticationProvider(Configuration.Search.GitHub.AppId,
                                            GitHubScope,
                                            new List<string> { Configuration.Search.GitHub.ApiBaseUrl.Host },
                                            httpClient,
                                            DisplayGitHubDeviceCodeLoginMessage,
                                            logger);
    private IAuthenticationProvider GetGitHubPatAuthenticationProvider(ILogger logger) =>
        new PatAuthenticationProvider(Configuration.Search.GitHub.AppId,
                                    GitHubScope,
                                    new List<string> { Configuration.Search.GitHub.ApiBaseUrl.Host },
                                    logger,
                                    GetGitHubPatStorageService(logger));
    protected async Task<KiotaSearcher> GetKiotaSearcherAsync(ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger<KiotaSearcher>();
        var deviceCodeSignInCallback = GetIsGitHubDeviceSignedInCallback(logger);
        var patSignInCallBack = GetIsGitHubPatSignedInCallback(logger);
        var isDeviceCodeSignedIn = await deviceCodeSignInCallback(cancellationToken).ConfigureAwait(false);
        var isPatSignedIn = await patSignInCallBack(cancellationToken).ConfigureAwait(false);
        var (provider, callback) = (isDeviceCodeSignedIn, isPatSignedIn) switch
        {
            (true, _) => (GetGitHubAuthenticationProvider(logger), deviceCodeSignInCallback),
            (_, true) => (GetGitHubPatAuthenticationProvider(logger), patSignInCallBack),
            (_, _) => (null, (CancellationToken cts) => Task.FromResult(false))
        };
        return new KiotaSearcher(logger, Configuration.Search, httpClient, provider, callback);
    }
    public int Invoke(InvocationContext context)
    {
        throw new InvalidOperationException("This command handler is async only");
    }
    public abstract Task<int> InvokeAsync(InvocationContext context);
    protected (ILoggerFactory, ILogger<T>) GetLoggerAndFactory<T>(InvocationContext context)
    {
        LogLevel logLevel = context.ParseResult.GetValueForOption(LogLevelOption);
#if DEBUG
        logLevel = logLevel > LogLevel.Debug ? LogLevel.Debug : logLevel;
#endif

        var loggerFactory = LoggerFactory.Create(builder =>
        {
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
    protected static string GetAbsolutePath(string source)
    {
        if (string.IsNullOrEmpty(source))
            return string.Empty;
        return Path.IsPathRooted(source) || source.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? source : NormalizeSlashesInPath(Path.Combine(Directory.GetCurrentDirectory(), source));
    }
    protected void AssignIfNotNullOrEmpty(string input, Action<GenerationConfiguration, string> assignment)
    {
        if (!string.IsNullOrEmpty(input))
            assignment.Invoke(Configuration.Generation, input);
    }
    protected static string NormalizeSlashesInPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            return path.Replace('/', '\\');
        return path.Replace('\\', '/');
    }
    private readonly Lazy<bool> tutorialMode = new(() =>
    {
        var kiotaInContainerRaw = Environment.GetEnvironmentVariable("KIOTA_TUTORIAL_ENABLED");
        if (!string.IsNullOrEmpty(kiotaInContainerRaw) && bool.TryParse(kiotaInContainerRaw, out var kiotaTutorial))
        {
            return kiotaTutorial;
        }
        return true;
    });
    protected bool TutorialMode => tutorialMode.Value;
    private readonly Lazy<bool> consoleSwapColors = new(() =>
    {
        var kiotaSwapColorsRaw = Environment.GetEnvironmentVariable("KIOTA_CONSOLE_COLORS_SWAP");
        if (!string.IsNullOrEmpty(kiotaSwapColorsRaw) && bool.TryParse(kiotaSwapColorsRaw, out var kiotaSwapColors))
        {
            return kiotaSwapColors;
        }
        return false;
    });
    protected bool SwapColors => consoleSwapColors.Value;
    private readonly Lazy<bool> consoleNoColors = new(() =>
    {
        var kiotaNoColorsRaw = Environment.GetEnvironmentVariable("KIOTA_CONSOLE_COLORS_ENABLED");
        if (!string.IsNullOrEmpty(kiotaNoColorsRaw) && bool.TryParse(kiotaNoColorsRaw, out var kiotaNoColors))
        {
            return kiotaNoColors;
        }
        return true;
    });
    protected bool ColorsEnabled => consoleNoColors.Value;

    private void DisplayHint(params string[] messages)
    {
        if (TutorialMode)
        {
            Console.WriteLine();
            DisplayMessages(ConsoleColor.Blue, messages);
        }
    }
    private void DisplayMessages(ConsoleColor color, params string[] messages)
    {
        if (SwapColors)
            color = Enum.GetValues<ConsoleColor>()[ConsoleColor.White - color];
        if (ColorsEnabled)
            Console.ForegroundColor = color;
        foreach (var message in messages)
            Console.WriteLine(message);
        if (ColorsEnabled)
            Console.ResetColor();
    }
    protected void DisplayError(params string[] messages)
    {
        DisplayMessages(ConsoleColor.Red, messages);
    }
    protected void DisplayWarning(params string[] messages)
    {
        DisplayMessages(ConsoleColor.Yellow, messages);
    }
    protected void DisplaySuccess(params string[] messages)
    {
        DisplayMessages(ConsoleColor.Green, messages);
    }
    protected void DisplayInfo(params string[] messages)
    {
        DisplayMessages(ConsoleColor.White, messages);
    }
    protected void DisplayDownloadHint(string searchTerm, string version)
    {
        var example = string.IsNullOrEmpty(version) ?
            $"Example: kiota download {searchTerm} -o <output path>" :
            $"Example: kiota download {searchTerm} -v {version} -o <output path>";
        DisplayHint("Hint: use kiota download to download the OpenAPI description.", example);
    }
    protected void DisplayShowHint(string searchTerm, string version, string? path = null)
    {
        var example = path switch
        {
            _ when !string.IsNullOrEmpty(path) => $"Example: kiota show -d {path}",
            _ when string.IsNullOrEmpty(version) => $"Example: kiota show -k {searchTerm}",
            _ => $"Example: kiota show -k {searchTerm} -v {version}",
        };
        DisplayHint("Hint: use kiota show to display a tree of paths present in the OpenAPI description.", example);
    }
    protected void DisplayShowAdvancedHint(string searchTerm, string version, IEnumerable<string> includePaths, IEnumerable<string> excludePaths, string? path = null)
    {
        if (!includePaths.Any() && !excludePaths.Any())
        {
            var example = path switch
            {
                _ when !string.IsNullOrEmpty(path) => $"Example: kiota show -d {path} --include-path **/foo",
                _ when string.IsNullOrEmpty(version) => $"Example: kiota show -k {searchTerm} --include-path **/foo",
                _ => $"Example: kiota show -k {searchTerm} -v {version} --include-path **/foo",
            };
            DisplayHint("Hint: use the --include-path and --exclude-path options with glob patterns to filter the paths displayed.", example);
        }
    }
    protected void DisplaySearchAddHint()
    {
        DisplayHint("Hint: add your own API to the search result https://aka.ms/kiota/addapi.");
    }
    protected void DisplaySearchHint(string? firstKey, string version)
    {
        if (!string.IsNullOrEmpty(firstKey))
        {
            var example = string.IsNullOrEmpty(version) ?
                $"Example: kiota search {firstKey}" :
                $"Example: kiota search {firstKey} -v {version}";
            DisplayHint("Hint: multiple matches found, use the key as the search term to display the details of a specific description.", example);
        }
    }
    protected void DisplayGenerateHint(string path, IEnumerable<string> includedPaths, IEnumerable<string> excludedPaths)
    {
        var includedPathsSuffix = (includedPaths.Any() ? " -i " : string.Empty) + string.Join(" -i ", includedPaths);
        var excludedPathsSuffix = (excludedPaths.Any() ? " -e " : string.Empty) + string.Join(" -e ", excludedPaths);
        var example = $"Example: kiota generate -l <language> -o <output path> -d {path}{includedPathsSuffix}{excludedPathsSuffix}";
        DisplayHint("Hint: use kiota generate to generate a client for the OpenAPI description.", example);
    }
    protected void DisplayGenerateAdvancedHint(IEnumerable<string> includePaths, IEnumerable<string> excludePaths, string path)
    {
        if (!includePaths.Any() && !excludePaths.Any())
        {
            DisplayHint("Hint: use the --include-path and --exclude-path options with glob patterns to filter the paths generated.",
                        $"Example: kiota generate --include-path **/foo -d {path}");
        }
    }
    protected void DisplayInfoHint(GenerationLanguage language, string path)
    {
        DisplayHint("Hint: use the info command to get the list of dependencies you need to add to your project.",
                    $"Example: kiota info -d {path} -l {language}");
    }
    protected void DisplayCleanHint(string commandName)
    {
        DisplayHint("Hint: to force the generation to overwrite an existing client pass the --clean-output switch.",
                    $"Example: kiota {commandName} --clean-output");
    }
    protected void DisplayInfoAdvancedHint()
    {
        DisplayHint("Hint: use the language argument to get the list of dependencies you need to add to your project.",
                    "Example: kiota info -l <language>");
    }
    protected void DisplayGitHubLogoutHint()
    {
        DisplayHint("Hint: use the logout command to sign out of GitHub.",
                    "Example: kiota logout github");
    }
    protected void DisplayManageInstallationHint()
    {
        DisplayHint($"Hint: go to {Configuration.Search.GitHub.AppManagement} to manage your which organizations and repositories Kiota has access to.");
    }
    protected void DisplaySearchBasicHint()
    {
        DisplayHint("Hint: use the search command to search for an OpenAPI description.",
                    "Example: kiota search <search term>");
    }
    protected async Task DisplayLoginHintAsync(ILogger logger, CancellationToken token)
    {
        var deviceCodeAuthProvider = GetGitHubDeviceStorageService(logger);
        var patStorage = GetGitHubPatStorageService(logger);
        if (!await deviceCodeAuthProvider.TokenStorageService.Value.IsTokenPresentAsync(token) && !await patStorage.IsTokenPresentAsync(token))
        {
            DisplayHint("Hint: use the login command to sign in to GitHub and access private OpenAPI descriptions.",
                        "Example: kiota login github");
        }
    }
    protected void DisplayGitHubDeviceCodeLoginMessage(Uri uri, string code)
    {
        DisplayInfo($"Please go to {uri} and enter the code {code} to authenticate.");
    }
}
