package com.microsoft.kiota.authentication;

import com.microsoft.kiota.RequestInfo;

import java.lang.UnsupportedOperationException;
import java.util.concurrent.CompletableFuture;
import java.util.Objects;

import javax.annotation.Nonnull;

/** Provides a base class for implementing AuthenticationProvider for Bearer token scheme. */
public abstract class BaseBearerTokenAuthenticationProvider implements AuthenticationProvider {
    private final static String authorizationHeaderKey = "Authorization";
    public CompletableFuture<Void> authenticateRequest(final RequestInfo request) {
        Objects.requireNonNull(request);
        if(!request.headers.keySet().contains(authorizationHeaderKey)) {
            return this.getAuthorizationToken(request)
                .thenApply(token -> {
                    if(token == null || token.isEmpty()) {
                        throw new UnsupportedOperationException("Could not get an authorization token", null);
                    }
                    request.headers.put(authorizationHeaderKey, "Bearer " + token);
                    return null;
                });
        } else {
            return CompletableFuture.completedFuture(null);
        }
    }
    /**
     * This method is called by the BaseBearerTokenAuthenticationProvider class to authenticate the request via the returned access token.
     * @param request The request to authenticate.
     * @return A CompletableFuture that holds the access token to use for the request.
     */
    @Nonnull
    public abstract CompletableFuture<String> getAuthorizationToken(@Nonnull final RequestInfo request);
}