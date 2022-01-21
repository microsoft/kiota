package com.microsoft.kiota.authentication;

import com.microsoft.kiota.RequestInformation;

import java.lang.UnsupportedOperationException;
import java.net.URI;
import java.net.URISyntaxException;
import java.util.concurrent.CompletableFuture;
import java.util.Objects;

import javax.annotation.Nonnull;

/** Provides a base class for implementing AuthenticationProvider for Bearer token scheme. */
public class BaseBearerTokenAuthenticationProvider implements AuthenticationProvider {
    public BaseBearerTokenAuthenticationProvider(@Nonnull final AccessTokenProvider accessTokenProvider) {
        this.accessTokenProvider = Objects.requireNonNull(accessTokenProvider);
    }
    private final AccessTokenProvider accessTokenProvider;
    private final static String authorizationHeaderKey = "Authorization";
    public CompletableFuture<Void> authenticateRequest(final RequestInformation request) {
        Objects.requireNonNull(request);
        if(!request.headers.keySet().contains(authorizationHeaderKey)) {
            final URI targetUri;
            try {
                targetUri = request.getUri();
            } catch (URISyntaxException e) {
                return CompletableFuture.failedFuture(e);
            }
            return this.accessTokenProvider.getAuthorizationToken(targetUri)
                .thenApply(token -> {
                    if(!(token == null || token.isEmpty())) { 
                    // Not an error, just no need to authenticate as we might have been given an external URL from the main API (large file upload, etc.)
                        request.headers.put(authorizationHeaderKey, "Bearer " + token);
                    }
                    return null;
                });
        } else {
            return CompletableFuture.completedFuture(null);
        }
    }
}