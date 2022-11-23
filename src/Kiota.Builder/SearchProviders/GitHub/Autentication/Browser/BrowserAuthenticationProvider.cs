using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Kiota.Builder.SearchProviders.GitHub.Authentication.Browser;

public class BrowserAuthenticationProvider : BaseAuthenticationProvider<AccessTokenProvider>
{
    public BrowserAuthenticationProvider(string clientId, string scope, IEnumerable<string> validHosts, HttpClient httpClient, Action<Uri, string> redirectCallback, ILogger logger, string authorizationCode, Uri redirectUri) :
        base(clientId, scope, validHosts, logger, (clientId, scope, validHosts) => new AccessTokenProvider {
            ClientId = clientId,
            HttpClient = httpClient,
            RedirectCallback = redirectCallback,
            Scope = scope,
            AllowedHostsValidator = new AllowedHostsValidator(validHosts),
            AuthorizationCode = authorizationCode,
            RedirectUri = redirectUri,
        })
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(redirectCallback);
        ArgumentNullException.ThrowIfNull(redirectUri);
    }
}
