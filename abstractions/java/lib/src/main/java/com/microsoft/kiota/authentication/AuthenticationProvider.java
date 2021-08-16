package com.microsoft.kiota.authentication;

import com.microsoft.kiota.RequestInfo;

import java.util.concurrent.CompletableFuture;

import javax.annotation.Nonnull;

/** Authenticates the application and returns a token. */
public interface AuthenticationProvider {
    /**
     * Authenticates the application request.
     * @param request the request to authenticate.
     * @return a CompletableFuture to await for the authentication to be completed.
     */
    @Nonnull
    CompletableFuture<Void> authenticateRequest(@Nonnull final RequestInfo request);
}
