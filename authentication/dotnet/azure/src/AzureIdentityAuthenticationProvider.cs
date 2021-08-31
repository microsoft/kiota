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

namespace Microsoft.Kiota.Authentication.Azure
{
    /// <summary>
    /// The <see cref="BaseBearerTokenAuthenticationProvider"/> implementation that supports implementations of <see cref="TokenCredential"/> from Azure.Identity.
    /// </summary>
    public class AzureIdentityAuthenticationProvider : BaseBearerTokenAuthenticationProvider
    {
        private readonly TokenCredential _credential;
        private readonly List<string> _scopes;

        /// <summary>
        /// The <see cref="AzureIdentityAuthenticationProvider"/> constructor
        /// </summary>
        /// <param name="credentials"></param>
        /// <param name="scopes"></param>
        public AzureIdentityAuthenticationProvider(TokenCredential credentials, params string[] scopes)
        {
            _credential = credentials ?? throw new ArgumentNullException(nameof(credentials));
            if(scopes == null)
                _scopes = new();
            else
                _scopes = scopes.ToList();

            if(!_scopes.Any())
                _scopes.Add("https://graph.microsoft.com/.default"); //TODO: init from the request hostname instead so it doesn't block national clouds?
        }

        /// <summary>
        /// Gets the authorization token for the given request.
        /// </summary>
        /// <param name="request">The <see cref="RequestInformation"/> instance to get te token for</param>
        /// <returns> An authorization token string.</returns>
        public async override Task<string> GetAuthorizationTokenAsync(RequestInformation request)
        {
            var result = await this._credential.GetTokenAsync(new TokenRequestContext(_scopes.ToArray()), default); //TODO: we might have to bubble that up for native apps or backend web apps to avoid blocking the UI/getting an exception
            return result.Token;
        }
    }
}
