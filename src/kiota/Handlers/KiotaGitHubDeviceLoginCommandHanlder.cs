using System;
using System.Collections.Generic;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using kiota.Authentication.GitHub.DeviceCode;
using Kiota.Builder.SearchProviders.GitHub.GitHubClient;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace kiota.Handlers;

internal class KiotaGitHubDeviceLoginCommandHandler : BaseKiotaCommandHandler
{
    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;
        var (loggerFactory, logger) = GetLoggerAndFactory<DeviceCodeAuthenticationProvider>(context);
        using (loggerFactory) {
            try {
                return await LoginAsync(logger, cancellationToken);
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
    private async Task<int> LoginAsync(ILogger logger, CancellationToken cancellationToken) {
        var authenticationProvider = new DeviceCodeAuthenticationProvider(Configuration.Search.GitHub.AppId,
                                                                        "repo",
                                                                        new List<string> { "api.github.com"},
                                                                        httpClient,
                                                                        DisplayGitHubDeviceCodeLoginMessage,
                                                                        logger);
        var dummyRequest = new RequestInformation() {
            HttpMethod = Method.GET,
            URI = Configuration.Search.GitHub.ApiBaseUrl,
        };
        await authenticationProvider.AuthenticateRequestAsync(dummyRequest, cancellationToken: cancellationToken);
        if(dummyRequest.Headers.TryGetValue("Authorization", out var authHeaderValue) && authHeaderValue.FirstOrDefault() is string authHeader && authHeader.StartsWith("bearer", StringComparison.OrdinalIgnoreCase)) {
            DisplaySuccess("Authentication successful.");
            await ListOutRepositoriesAsync(authenticationProvider, cancellationToken);
            DisplayManageInstallationHint();
            DisplaySearchBasicHint();
            DisplayGitHubLogoutHint();
            return 0;
        } else {
            DisplayError("Authentication failed. Please try again.");
            return 1;
        }
    }
    private async Task ListOutRepositoriesAsync(IAuthenticationProvider authProvider, CancellationToken cancellationToken) {
        var requestAdapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient);
        var client = new GitHubClient(requestAdapter);
        var installations = await client.User.Installations.GetAsync(cancellationToken: cancellationToken);
        if (installations?.Total_count > 0 && installations.Installations != null) {
            DisplayInfo("Kiota is installed to the following organizations/accounts:");
            foreach (var installation in installations.Installations)
                DisplayInfo($"- {installation?.Account?.SimpleUser?.Login} ({installation?.Repository_selection} repositories){(installation?.Suspended_at != null ? " (suspended)" : string.Empty)}");
        } else {
            DisplayWarning("Kiota is not installed to any GitHub organization/account.");
        }
    }
}
