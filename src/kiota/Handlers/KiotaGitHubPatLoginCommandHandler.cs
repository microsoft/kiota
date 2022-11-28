using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.SearchProviders.GitHub.Authentication;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers;
internal class KiotaGitHubPatLoginCommandHandler : BaseKiotaCommandHandler {
    public required Option<string> PatOption { get; init; }
    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;
        string pat = context.ParseResult.GetValueForOption(PatOption) ?? string.Empty;
        var (loggerFactory, logger) = GetLoggerAndFactory<PatAuthenticationProvider>(context);
        using (loggerFactory) {
            try {
                return await LoginAsync(logger, pat, cancellationToken);
            } catch (Exception ex) {
    #if DEBUG
                logger.LogCritical(ex, "error signing in to GitHub: {exceptionMessage}", ex.Message);
                throw; // so debug tools go straight to the source of the exception when attached
    #else
                logger.LogCritical("error signing in to GitHub: {exceptionMessage}", ex.Message);
                return 1;
    #endif
            }
        }
    }
    private async Task<int> LoginAsync(ILogger logger, string patValue, CancellationToken cancellationToken) {
        if(string.IsNullOrEmpty(patValue)) {
            logger.LogCritical("no personal access token provided");
            return 1;
        }
        await GetGitHubPatStorageService(logger).SetTokenAsync(patValue, cancellationToken).ConfigureAwait(false);
        DisplaySuccess("Authentication successful.");
        DisplaySearchBasicHint();
        DisplayGitHubLogoutHint();
        return 0;
    }
}
