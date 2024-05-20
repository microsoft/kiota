﻿using System.CommandLine;
using System.CommandLine.Invocation;
using kiota.Authentication.GitHub.DeviceCode;
using Kiota.Builder;
using Kiota.Builder.Configuration;
using Kiota.Builder.Logging;
using Kiota.Builder.SearchProviders.GitHub.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions.Authentication;

namespace kiota.Handlers;

internal abstract class BaseKiotaCommandHandler : ICommandHandler, IDisposable
{
    protected static void DefaultSerializersAndDeserializers(GenerationConfiguration generationConfiguration)
    { // needed until we have rollup packages
        var defaultGenerationConfiguration = new GenerationConfiguration();
        generationConfiguration.Serializers = defaultGenerationConfiguration.Serializers;
        generationConfiguration.Deserializers = defaultGenerationConfiguration.Deserializers;
    }
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

    private HttpClient? _httpClient;
    protected HttpClient httpClient
    {
        get
        {
            _httpClient ??= GetHttpClient();
            return _httpClient;
        }
    }
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

    protected HttpClient GetHttpClient()
    {
        var httpClientHandler = new HttpClientHandler();
        if (Configuration.Generation.DisableSSLValidation)
            httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        var httpClient = new HttpClient(httpClientHandler);

        disposables.Add(httpClientHandler);
        disposables.Add(httpClient);

        return httpClient;
    }

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
    protected async Task CheckForNewVersionAsync(ILogger logger, CancellationToken cancellationToken)
    {
        if (Configuration.Update.Disabled)
        {
            return;
        }

        var updateService = new UpdateService(httpClient, logger, Configuration.Update);
        var result = await updateService.GetUpdateMessageAsync(Kiota.Generated.KiotaVersion.Current(), cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(result))
            DisplayWarning(result);
    }
    public abstract Task<int> InvokeAsync(InvocationContext context);
    private readonly List<IDisposable> disposables = new();
    protected (ILoggerFactory, ILogger<T>) GetLoggerAndFactory<T>(InvocationContext context, string logFileRootPath = "")
    {
        LogLevel logLevel = context.ParseResult.GetValueForOption(LogLevelOption);
#if DEBUG
        logLevel = logLevel > LogLevel.Debug ? LogLevel.Debug : logLevel;
#endif
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            var logFileAbsoluteRootPath = GetAbsolutePath(logFileRootPath);
            var fileLogger = new FileLogLoggerProvider(logFileAbsoluteRootPath, logLevel);
            disposables.Add(fileLogger);
            builder
                .AddConsole()
#if DEBUG
                .AddDebug()
#endif
                .AddProvider(fileLogger)
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
            _ when !string.IsNullOrEmpty(path) => $"Example: kiota show -d \"{path}\"",
            _ when string.IsNullOrEmpty(version) => $"Example: kiota show -k {searchTerm}",
            _ => $"Example: kiota show -k {searchTerm} -v {version}",
        };
        DisplayHint("Hint: use kiota show to display a tree of paths present in the OpenAPI description.", example);
    }
    protected void DisplayShowAdvancedHint(string searchTerm, string version, IEnumerable<string> includePaths, IEnumerable<string> excludePaths, string? path = null, string? manifest = null)
    {
        if (!includePaths.Any() && !excludePaths.Any())
        {
            var example = path switch
            {
                _ when !string.IsNullOrEmpty(path) => $"Example: kiota show -d \"{path}\" --include-path \"**/foo\"",
                _ when !string.IsNullOrEmpty(manifest) => $"Example: kiota show -m \"{manifest}\" --include-path \"**/foo\"",
                _ when string.IsNullOrEmpty(version) => $"Example: kiota show -k {searchTerm} --include-path \"**/foo\"",
                _ => $"Example: kiota show -k {searchTerm} -v {version} --include-path \"**/foo\"",
            };
            DisplayHint("Hint: use the --include-path and --exclude-path options with glob patterns to filter the paths displayed.", example);
        }
    }
    protected void DisplayGenerateAfterMigrateHint()
    {
        DisplayHint("Hint: use the generate command to update the client and the manifest requests.",
                    "Example: kiota client generate");
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
    protected void DisplayGenerateHint(string path, string manifest, IEnumerable<string> includedPaths, IEnumerable<string> excludedPaths)
    {
        var includedPathsSuffix = (includedPaths.Any() ? " -i " : string.Empty) + string.Join(" -i ", includedPaths.Select(static x => $"\"{x}\""));
        var excludedPathsSuffix = (excludedPaths.Any() ? " -e " : string.Empty) + string.Join(" -e ", excludedPaths.Select(static x => $"\"{x}\""));
        var sourceArg = GetSourceArg(path, manifest);
        var example = $"Example: kiota generate -l <language> -o <output path> {sourceArg}{includedPathsSuffix}{excludedPathsSuffix}";
        DisplayHint("Hint: use kiota generate to generate a client for the OpenAPI description.", example);
    }
    protected void DisplayGenerateAdvancedHint(IEnumerable<string> includePaths, IEnumerable<string> excludePaths, string path, string manifest, string commandName = "generate")
    {
        if (!includePaths.Any() && !excludePaths.Any())
        {
            var sourceArg = GetSourceArg(path, manifest);
            DisplayHint("Hint: use the --include-path and --exclude-path options with glob patterns to filter the paths generated.",
                        $"Example: kiota {commandName} --include-path \"**/foo\" {sourceArg}");
        }
    }
    private static string GetSourceArg(string path, string manifest)
    {
        return string.IsNullOrEmpty(manifest) ? $"-d \"{path}\"" : $"-a \"{manifest}\"";
    }
    protected void DisplayUrlInformation(string? apiRootUrl, bool isPlugin = false)
    {
        if (!string.IsNullOrEmpty(apiRootUrl))
            DisplayInfo($"{(isPlugin ? "Plugin" : "Client")} base url set to {apiRootUrl}");
    }
    protected void DisplayGenerateCommandHint()
    {
        DisplayHint("Hint: use the client generate command to generate the code.",
                    "Example: kiota client generate");
    }
    protected void DisplayInfoHint(GenerationLanguage language, string path, string manifest)
    {
        var sourceArg = GetSourceArg(path, manifest);
        DisplayHint("Hint: use the info command to get the list of dependencies you need to add to your project.",
                    $"Example: kiota info {sourceArg} -l {language}");
    }
    protected void DisplayInstallHint(LanguageInformation languageInformation, List<LanguageDependency> languageDependencies)
    {
        if (!string.IsNullOrEmpty(languageInformation.DependencyInstallCommand) && languageDependencies.Count > 0)
        {
            string[] fixedLines = [$"Hint: use the install command to install the dependencies.",
                $"Example: "];
            DisplayHint(fixedLines.Union(
                    languageDependencies.Select(x => "   " + string.Format(languageInformation.DependencyInstallCommand, x.Name, x.Version))).ToArray());
        }
    }
    protected void DisplayCleanHint(string commandName, string argumentName = "--clean-output")
    {
        DisplayHint($"Hint: to force the generation to overwrite an existing client pass the {argumentName} switch.",
                    $"Example: kiota {commandName} {argumentName}");
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
    protected void WarnShouldUseKiotaConfigClientsCommands()
    {
        if (KiotaHost.IsConfigPreviewEnabled.Value)
            DisplayWarning("Warning: the kiota generate and update commands are deprecated, use kiota client commands instead.");
    }
    protected void WarnUsingPreviewLanguage(GenerationLanguage language)
    {
        if (Configuration.Languages.TryGetValue(language.ToString(), out var languageInformation) && languageInformation.MaturityLevel is not LanguageMaturityLevel.Stable)
            DisplayWarning($"Warning: the {language} language is in preview ({languageInformation.MaturityLevel}) some features are not fully supported and source breaking changes will happen with future updates.");
    }
    protected void DisplayGitHubDeviceCodeLoginMessage(Uri uri, string code)
    {
        DisplayInfo($"Please go to {uri} and enter the code {code} to authenticate.");
    }
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
            return;
        foreach (var disposable in disposables)
            disposable.Dispose();
    }
}
