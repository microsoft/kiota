using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.Diagnostics;
using kiota.Authentication.GitHub.DeviceCode;
using kiota.Extension;
using kiota.Telemetry;
using Kiota.Builder.SearchProviders.GitHub.GitHubClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace kiota.Handlers;

internal class KiotaGitHubDeviceLoginCommandHandler : BaseKiotaCommandHandler
{
    private readonly KeyValuePair<string, object?>[] _commonTags =
    [
        new(TelemetryLabels.TagCommandName, "login-github-device"),
        new(TelemetryLabels.TagCommandRevision, 1)
    ];
    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        // Span start time
        Stopwatch? stopwatch = Stopwatch.StartNew();
        var startTime = DateTimeOffset.UtcNow;
        // Get options
        var logLevel = context.ParseResult.FindResultFor(LogLevelOption)?.GetValueOrDefault() as LogLevel?;
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;

        var host = context.GetHost();
        var instrumentation = host.Services.GetService<Instrumentation>();
        var activitySource = instrumentation?.ActivitySource;

        CreateTelemetryTags(activitySource, logLevel, out var tags);
        // Start span
        using var invokeActivity = activitySource?.StartActivity(ActivityKind.Internal, name: TelemetryLabels.SpanGitHubDeviceLoginCommand,
            startTime: startTime,
            tags: _commonTags.ConcatNullable(tags)?.Concat(Telemetry.Telemetry.GetThreadTags()));
        // Command duration meter
        var meterRuntime = instrumentation?.CreateCommandDurationHistogram();
        if (meterRuntime is null) stopwatch = null;
        // Add this run to the command execution counter
        var tl = new TagList(_commonTags.AsSpan()).AddAll(tags.OrEmpty());
        instrumentation?.CreateCommandExecutionCounter().Add(1, tl);

        var (loggerFactory, logger) = GetLoggerAndFactory<DeviceCodeAuthenticationProvider>(context);
        using (loggerFactory)
        {
            await CheckForNewVersionAsync(logger, cancellationToken).ConfigureAwait(false);
            try
            {
                var result = await LoginAsync(logger, cancellationToken).ConfigureAwait(false);
                invokeActivity?.SetStatus(ActivityStatusCode.Ok);
                return result;
            }
            catch (Exception ex)
            {
                invokeActivity?.SetStatus(ActivityStatusCode.Error);
                invokeActivity?.AddException(ex);
#if DEBUG
                logger.LogCritical(ex, "error signing in to GitHub: {exceptionMessage}", ex.Message);
                throw; // so debug tools go straight to the source of the exception when attached
#else
                logger.LogCritical("error signing in to GitHub: {exceptionMessage}", ex.Message);
                return 1;
#endif
            }
            finally
            {
                if (stopwatch is not null) meterRuntime?.Record(stopwatch.Elapsed.TotalSeconds, tl);
            }
        }
    }
    private async Task<int> LoginAsync(ILogger logger, CancellationToken cancellationToken)
    {
        var authenticationProvider = new DeviceCodeAuthenticationProvider(Configuration.Search.GitHub.AppId,
                                                                        "repo",
                                                                        new List<string> { "api.github.com" },
                                                                        httpClient,
                                                                        DisplayGitHubDeviceCodeLoginMessage,
                                                                        logger);
        var dummyRequest = new RequestInformation()
        {
            HttpMethod = Method.GET,
            URI = Configuration.Search.GitHub.ApiBaseUrl,
        };
        await authenticationProvider.AuthenticateRequestAsync(dummyRequest, cancellationToken: cancellationToken);
        if (dummyRequest.Headers.TryGetValue("Authorization", out var authHeaderValue) && authHeaderValue.FirstOrDefault() is string authHeader && authHeader.StartsWith("bearer", StringComparison.OrdinalIgnoreCase))
        {
            DisplaySuccess("Authentication successful.");
            await ListOutRepositoriesAsync(authenticationProvider, cancellationToken);
            DisplayManageInstallationHint();
            DisplaySearchBasicHint();
            DisplayGitHubLogoutHint();
            return 0;
        }
        else
        {
            DisplayError("Authentication failed. Please try again.");
            return 1;
        }
    }
    private async Task ListOutRepositoriesAsync(IAuthenticationProvider authProvider, CancellationToken cancellationToken)
    {
        var requestAdapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient);
        var client = new GitHubClient(requestAdapter);
        var installations = await client.User.Installations.GetAsync(cancellationToken: cancellationToken);
        if (installations?.TotalCount > 0 && installations.Installations != null)
        {
            DisplayInfo("Kiota is installed to the following organizations/accounts:");
            foreach (var installation in installations.Installations)
                DisplayInfo($"- {installation?.Account?.SimpleUser?.Login} ({installation?.RepositorySelection} repositories){(installation?.SuspendedAt != null ? " (suspended)" : string.Empty)}");
        }
        else
        {
            DisplayWarning("Kiota is not installed to any GitHub organization/account.");
        }
    }

    private static void CreateTelemetryTags(ActivitySource? activitySource, LogLevel? logLevel,
        out List<KeyValuePair<string, object?>>? tags)
    {
        // set up telemetry tags
        tags = activitySource?.HasListeners() == true ? new List<KeyValuePair<string, object?>>(2)
        {
            new(TelemetryLabels.TagCommandSource, TelemetryLabels.CommandSourceCliValue),
        } : null;
        if (logLevel is { } ll) tags?.Add(new KeyValuePair<string, object?>($"{TelemetryLabels.TagCommandParams}.log_level", ll.ToString("G")));
    }
}
