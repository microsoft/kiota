using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.SearchProviders.GitHub.Authentication;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers;
internal class KiotaGitHubPatLoginCommandHandler : BaseKiotaCommandHandler {
    public Option<string> PatOption { get; set; }
    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        CancellationToken cancellationToken = (CancellationToken)context.BindingContext.GetService(typeof(CancellationToken));
        string pat = context.ParseResult.GetValueForOption(PatOption);
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
        var tokenStorageService = new TempFolderTokenStorageService {
            Logger = logger,
            FileName = "pat-api.github.com"
        };
        await tokenStorageService.SetTokenAsync(patValue, cancellationToken).ConfigureAwait(false);
        DisplaySuccess("Authentication successful.");
        DisplaySearchBasicHint();
        DisplayGitHubLogoutHint();
        return 0;
    }
}
