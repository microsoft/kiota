package com.microsoft.kiota.authentication;

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
import com.microsoft.kiota.authentication.BaseBearerTokenAuthenticationProvider;
import com.microsoft.kiota.RequestInfo;

public class AzureIdentityAuthenticationProvider extends BaseBearerTokenAuthenticationProvider {
    private final TokenCredential creds;
    private final List<String> _scopes;
    public AzureIdentityAuthenticationProvider(@Nonnull final TokenCredential tokenCredential, @Nonnull final String... scopes) {
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
    public CompletableFuture<String> getAuthorizationToken(@Nonnull final RequestInfo request) {
        return this.creds.getToken(new TokenRequestContext() {{
            this.setScopes(_scopes);
        }}).toFuture().thenApply(r -> r.getToken());
    }
}
