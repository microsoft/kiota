using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Kiota.Builder.SearchProviders.GitHub.Authentication;

public class BaseAuthenticationProvider<T> : AnonymousAuthenticationProvider where T: class, IAccessTokenProvider
{
	public BaseAuthenticationProvider(string clientId,
        string scope,
        IEnumerable<string> validHosts,
        ILogger logger,
        Func<string, string, IEnumerable<string>, T> accessTokenProviderFactory,
        bool enableCache = true)
	{
        ArgumentNullException.ThrowIfNull(validHosts);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(accessTokenProviderFactory);
        if (string.IsNullOrEmpty(clientId))
			throw new ArgumentNullException(nameof(clientId));
		if (string.IsNullOrEmpty(scope))
			throw new ArgumentNullException(nameof(scope));

        AccessTokenProvider = accessTokenProviderFactory(clientId, scope, validHosts);
        if(enableCache)
            AccessTokenProvider = new TempFolderCachingAccessTokenProvider {
                Concrete = AccessTokenProvider,
                Logger = logger,
                ApiBaseUrl = new Uri($"https://{validHosts.FirstOrDefault() ?? "api.github.com"}"),
                AppId = clientId,
            };
	}
	public IAccessTokenProvider AccessTokenProvider {get; private set;}
    private const string AuthorizationHeaderKey = "Authorization";
    private const string ClaimsKey = "claims";
	public override Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object> additionalAuthenticationContext = null, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(request);
        return AuthenticateRequestInternalAsync(request, additionalAuthenticationContext, cancellationToken);
    }
	private async Task AuthenticateRequestInternalAsync(RequestInformation request, Dictionary<string, object> additionalAuthenticationContext = null, CancellationToken cancellationToken = default) {
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
