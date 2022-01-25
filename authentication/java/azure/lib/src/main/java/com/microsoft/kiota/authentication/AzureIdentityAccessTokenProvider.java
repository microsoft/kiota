package com.microsoft.kiota.authentication;

import java.net.URI;
import java.util.List;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Objects;
import java.util.concurrent.CompletableFuture;

import javax.annotation.Nonnull;

import com.azure.core.credential.TokenCredential;
import com.azure.core.credential.TokenRequestContext;

import com.microsoft.kiota.authentication.AccessTokenProvider;

/** Implementation of AccessTokenProvider that supports implementations of TokenCredential from Azure.Identity. */
public class AzureIdentityAccessTokenProvider implements AccessTokenProvider {
    private final TokenCredential creds;
    private final List<String> _scopes;
    /**
     * Creates a new instance of AzureIdentityAccessTokenProvider.
     * @param tokenCredential The Azure.Identity.TokenCredential implementation to use.
     * @param scopes The scopes to request access tokens for.
     */
    public AzureIdentityAccessTokenProvider(@Nonnull final TokenCredential tokenCredential, @Nonnull final String... scopes) {
        creds = Objects.requireNonNull(tokenCredential, "parameter tokenCredential cannot be null");

        if(scopes == null) {
            _scopes = new ArrayList<String>();
        } else if(scopes.length == 0) {
            _scopes = Arrays.asList(new String[] { "https://graph.microsoft.com/.default" });
        } else {
            _scopes = Arrays.asList(scopes);
        }
    }
    @Nonnull
    public CompletableFuture<String> getAuthorizationToken(@Nonnull final URI uri) {
        return this.creds.getToken(new TokenRequestContext() {{
            this.setScopes(_scopes);
        }}).toFuture().thenApply(r -> r.getToken());
    }
}
