﻿using Kiota.Builder.SearchProviders.GitHub.Authentication;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Kiota.Web.Authentication.GitHub;

public class BrowserAuthenticationProvider : BaseAuthenticationProvider<BrowserAccessTokenProvider>
{
    public BrowserAuthenticationProvider(string clientId, string scope, IEnumerable<string> validHosts, HttpClient httpClient, Func<Uri, string, CancellationToken, Task> redirectCallback, Func<CancellationToken, Task<string>> getAccessCodeCallback, ILogger logger, Uri redirectUri) :
        base(clientId, scope, validHosts, logger, (clientId, scope, validHosts) => new BrowserAccessTokenProvider
        {
            ClientId = clientId,
            HttpClient = httpClient,
            RedirectCallback = redirectCallback,
            GetAccessCodeCallback = getAccessCodeCallback,
            Scope = scope,
            AllowedHostsValidator = new AllowedHostsValidator(validHosts),
            RedirectUri = redirectUri,
        })
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(redirectCallback);
        ArgumentNullException.ThrowIfNull(redirectUri);
    }
}
