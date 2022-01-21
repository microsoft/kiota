// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Microsoft.Kiota.Authentication.Azure;
/// <summary>
/// Provides an implementation of <see cref="IAuthenticationProvider"/> for Azure.Identity.
/// </summary>
public class AzureIdentityAccessTokenProvider : IAccessTokenProvider
{
    private readonly TokenCredential _credential;
    private readonly List<string> _scopes;

    /// <summary>
    /// The <see cref="AzureIdentityAccessTokenProvider"/> constructor
    /// </summary>
    /// <param name="credential">The credential implementation to use to obtain the access token.</param>
    /// <param name="scopes">The scopes to request the access token for.</param>
    public AzureIdentityAccessTokenProvider(TokenCredential credential, params string[] scopes)
    {
        _credential = credential ?? throw new ArgumentNullException(nameof(credential));
        if(scopes == null)
            _scopes = new();
        else
            _scopes = scopes.ToList();

        if(!_scopes.Any())
            _scopes.Add("https://graph.microsoft.com/.default"); //TODO: init from the request hostname instead so it doesn't block national clouds?
    }

    /// <summary>
    /// Gets the authorization token for the given target URI.
    /// </summary>
    /// <param name="uri">The target <see cref="Uri"/> to get token for</param>
    /// <returns>An authorization token string.</returns>
    public async Task<string> GetAuthorizationTokenAsync(Uri uri)
    {
        var result = await this._credential.GetTokenAsync(new TokenRequestContext(_scopes.ToArray()), default); //TODO: we might have to bubble that up for native apps or backend web apps to avoid blocking the UI/getting an exception
        return result.Token;
    }

}
