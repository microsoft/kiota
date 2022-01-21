// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;

namespace Microsoft.Kiota.Abstractions.Authentication;
/// <summary>
///     Provides a base class for implementing <see cref="IAuthenticationProvider" /> for Bearer token scheme.
/// </summary>
public class BaseBearerTokenAuthenticationProvider : IAuthenticationProvider
{
    /// <summary>
    /// Creates a new instance of <see cref="BaseBearerTokenAuthenticationProvider"/>.
    /// </summary>
    /// <param name="accessTokenProvider">The <see cref="IAccessTokenProvider"/> to use for getting the access token.</param>
    public BaseBearerTokenAuthenticationProvider(IAccessTokenProvider accessTokenProvider)
    {
        AccessTokenProvider = accessTokenProvider ?? throw new ArgumentNullException(nameof(accessTokenProvider));
    }
    /// <summary>
    ///     Gets the <see cref="IAccessTokenProvider" /> to use for getting the access token.
    /// </summary>
    public IAccessTokenProvider AccessTokenProvider {get; private set;}
    private const string AuthorizationHeaderKey = "Authorization";

    /// <summary>
    /// Authenticates the <see cref="RequestInformation"/> instance
    /// </summary>
    /// <param name="request">The request to authenticate</param>
    /// <returns></returns>
    public async Task AuthenticateRequestAsync(RequestInformation request)
    {
        if(request == null) throw new ArgumentNullException(nameof(request));
        if(!request.Headers.ContainsKey(AuthorizationHeaderKey))
        {
            var token = await AccessTokenProvider.GetAuthorizationTokenAsync(request.URI);
            if(string.IsNullOrEmpty(token))
                throw new InvalidOperationException("Could not get an authorization token");
            request.Headers.Add(AuthorizationHeaderKey, $"Bearer {token}");
        }
    }
}
