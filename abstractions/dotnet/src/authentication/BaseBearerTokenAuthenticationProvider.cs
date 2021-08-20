using System;
using System.Threading.Tasks;

namespace Microsoft.Kiota.Abstractions.Authentication {
    /// <summary>
    ///     Provides a base class for implementing <see cref="IAuthenticationProvider" /> for Bearer token scheme.
    /// </summary>
    public abstract class BaseBearerTokenAuthenticationProvider : IAuthenticationProvider {
        private const string authorizationHeaderKey = "Authorization";
        public async Task AuthenticateRequestAsync(RequestInfo request) {
            if(request == null) throw new ArgumentNullException(nameof(request));
            if(!request.Headers.ContainsKey(authorizationHeaderKey)) {
                var token = await GetAuthorizationTokenAsync(request);
                if(string.IsNullOrEmpty(token))
                    throw new InvalidOperationException("Could not get an authorization token");
                request.Headers.Add(authorizationHeaderKey, $"Bearer {token}");
            }
        }
        /// <summary>
        ///     This method is called by the <see cref="BaseBearerTokenAuthenticationProvider" /> class to authenticate the request via the returned access token.
        /// </summary>
        /// <param name="request">The request to authenticate.</param>
        /// <returns>A Task that holds the access token to use for the request.</returns>
        public abstract Task<string> GetAuthorizationTokenAsync(RequestInfo request);
    }
}
