using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Kiota.Builder.SearchProviders.GitHub.Authentication.DeviceCode;

public class GitHubAuthenticationProvider : GitHubAnonymousAuthenticationProvider
{
	public GitHubAuthenticationProvider(string clientId, string scope, IEnumerable<string> validHosts, HttpClient httpClient, Action<Uri, string> messageCallback, ILogger logger)
	{
		if (string.IsNullOrEmpty(clientId))
			throw new ArgumentNullException(nameof(clientId));
		if (string.IsNullOrEmpty(scope))
			throw new ArgumentNullException(nameof(scope));
        ArgumentNullException.ThrowIfNull(validHosts);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(messageCallback);
        ArgumentNullException.ThrowIfNull(logger);

		AccessTokenProvider = new TempFolderCachingAccessTokenProvider {
            Concrete = new GitHubAccessTokenProvider {
                ClientId = clientId,
                Scope = scope,
                AllowedHostsValidator = new AllowedHostsValidator(validHosts),
                HttpClient = httpClient,
                MessageCallback = messageCallback
            },
            Logger = logger,
            ApiBaseUrl = new Uri($"https://{validHosts.FirstOrDefault() ?? "api.github.com"}"),
            AppId = clientId,
        };
	}
	public IAccessTokenProvider AccessTokenProvider {get; private set;}
    private const string AuthorizationHeaderKey = "Authorization";
    private const string ClaimsKey = "claims";
	public override async Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object> additionalAuthenticationContext = null, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(request);
        await base.AuthenticateRequestAsync(request, additionalAuthenticationContext, cancellationToken).ConfigureAwait(false);
        if(additionalAuthenticationContext != null &&
            additionalAuthenticationContext.ContainsKey(ClaimsKey) &&
            request.Headers.ContainsKey(AuthorizationHeaderKey))
            request.Headers.Remove(AuthorizationHeaderKey);

        if(!request.Headers.ContainsKey(AuthorizationHeaderKey))
        {
            var token = await AccessTokenProvider.GetAuthorizationTokenAsync(request.URI, additionalAuthenticationContext, cancellationToken);
            if(!string.IsNullOrEmpty(token))
                request.Headers.Add(AuthorizationHeaderKey, $"Bearer {token}");
        }
	}
}
