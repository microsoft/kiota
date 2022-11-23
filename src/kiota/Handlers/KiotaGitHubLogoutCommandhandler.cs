using System;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.SearchProviders.GitHub.Authentication;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers;

internal class KiotaGitHubLogoutCommandHandler : BaseKiotaCommandHandler
{
    public override Task<int> InvokeAsync(InvocationContext context)
    {
        var (loggerFactory, logger) = GetLoggerAndFactory<TempFolderCachingAccessTokenProvider>(context);
        using (loggerFactory) {
            try {
                var cachingProvider = GitHubAuthenticationCachingProvider(logger);
                var result = cachingProvider.Logout();
                if(result)
                    DisplaySuccess("Logged out successfully.");
                else
                    DisplaySuccess("Already logged out.");
                return Task.FromResult(0);
            } catch (Exception ex) {
    #if DEBUG
                logger.LogCritical(ex, "error logging out from GitHub: {exceptionMessage}", ex.Message);
                throw; // so debug tools go straight to the source of the exception when attached
    #else
                logger.LogCritical("error logging out from GitHub: {exceptionMessage}", ex.Message);
                return Task.FromResult(1);
    #endif
            }
        }
    }
}
