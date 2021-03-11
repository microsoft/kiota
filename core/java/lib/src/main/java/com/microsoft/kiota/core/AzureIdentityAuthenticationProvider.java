package com.microsoft.kiota.core;

import java.net.URI;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.Objects;
import java.util.concurrent.CompletableFuture;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

import com.azure.core.credential.TokenCredential;
import com.azure.core.credential.TokenRequestContext;
import com.microsoft.kiota.AuthenticationProvider;

public class AzureIdentityAuthenticationProvider implements AuthenticationProvider {
    private final TokenCredential creds;
    private final List<String> _scopes;
    public AzureIdentityAuthenticationProvider(@Nonnull final TokenCredential tokenCredential, @Nonnull final String... scopes) {
        creds = Objects.requireNonNull(tokenCredential, "parameter tokenCredential cannot be null");

        if(scopes == null) {
            _scopes = new ArrayList<String>();
        } else {
            _scopes = Arrays.asList(scopes);
        }
        if(scopes.length == 0) {
            _scopes.add("https://graph.microsoft.com/.default");
        }
    }

    @Nonnull
    public CompletableFuture<String> getAuthorizationToken(@Nonnull final URI requestUri) {
        return this.creds.getToken(new TokenRequestContext() {{
            this.setScopes(_scopes);
        }}).toFuture().thenApply(r -> r.getToken());
    }
}
