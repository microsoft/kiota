using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Kiota.Builder.SearchProviders.GitHub.Authentication.Browser;

public class BrowserAuthenticationProvider : BaseAuthenticationProvider<AccessTokenProvider>
{
    public BrowserAuthenticationProvider(string clientId, string scope, IEnumerable<string> validHosts, HttpClient httpClient, Func<Uri, string, CancellationToken, Task> redirectCallback, Func<CancellationToken, Task<string>> getAccessCodeCallback, ILogger logger, Uri redirectUri) :
        base(clientId, scope, validHosts, logger, (clientId, scope, validHosts) => new AccessTokenProvider {
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
