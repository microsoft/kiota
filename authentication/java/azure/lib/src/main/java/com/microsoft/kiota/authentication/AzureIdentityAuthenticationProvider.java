package com.microsoft.kiota.authentication;

import javax.annotation.Nonnull;


import com.azure.core.credential.TokenCredential;
import com.azure.core.credential.TokenRequestContext;
import com.microsoft.kiota.authentication.BaseBearerTokenAuthenticationProvider;

/** Implementation of AuthenticationProvider that supports implementations of TokenCredential from Azure.Identity. */
public class AzureIdentityAuthenticationProvider extends BaseBearerTokenAuthenticationProvider {
    /**
     * Creates a new instance of AzureIdentityAuthenticationProvider.
     * @param tokenCredential The Azure.Identity.TokenCredential implementation to use.
     * @param allowedHosts The list of allowed hosts for which to request access tokens.
     * @param scopes The scopes to request access tokens for.
     */
    public AzureIdentityAuthenticationProvider(@Nonnull final TokenCredential tokenCredential, @Nonnull final String[] allowedHosts, @Nonnull final String... scopes) {
        super(new AzureIdentityAccessTokenProvider(tokenCredential, allowedHosts, scopes));
    } 
}
