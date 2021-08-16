using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Microsoft.Kiota.Authentication.Azure {
    public class AzureIdentityAuthenticationProvider : BaseBearerTokenAuthenticationProvider
    {
        private readonly TokenCredential creds;
        private readonly List<string> _scopes;
        public AzureIdentityAuthenticationProvider(TokenCredential credentials, params string[] scopes)
        {
            creds = credentials ?? throw new ArgumentNullException(nameof(credentials));
            if(scopes == null)
                _scopes = new();
            else
                _scopes = scopes.ToList();

            if(!_scopes.Any())
                _scopes.Add("https://graph.microsoft.com/.default"); //TODO: init from the request hostname instead so it doesn't block national clouds?
                
        }
        public async override Task<string> GetAuthorizationToken(RequestInfo request)
        {
            var result = await this.creds.GetTokenAsync(new TokenRequestContext(_scopes.ToArray()), default); //TODO: we might have to bubble that up for native apps or backend web apps to avoid blocking the UI/getting an exception
            return result.Token;
        }
    }
}
