using System;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.SearchProviders.GitHub.Authentication;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers;

internal class KiotaGitHubLogoutCommandHandler : BaseKiotaCommandHandler
{
    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        CancellationToken cancellationToken = (CancellationToken)context.BindingContext.GetService(typeof(CancellationToken));
        var (loggerFactory, logger) = GetLoggerAndFactory<TempFolderCachingAccessTokenProvider>(context);
        using (loggerFactory) {
            try {
                var authProvider = GitHubDeviceAuthenticationProvider(logger);
                var result = await authProvider.TokenStorageService.Value.DeleteTokenAsync(cancellationToken);
                if(result)
                    DisplaySuccess("Logged out successfully.");
                else
                    DisplaySuccess("Already logged out.");
                return 0;
            } catch (Exception ex) {
    #if DEBUG
                logger.LogCritical(ex, "error logging out from GitHub: {exceptionMessage}", ex.Message);
                throw; // so debug tools go straight to the source of the exception when attached
    #else
                logger.LogCritical("error logging out from GitHub: {exceptionMessage}", ex.Message);
                return 1;
    #endif
            }
        }
    }
}
