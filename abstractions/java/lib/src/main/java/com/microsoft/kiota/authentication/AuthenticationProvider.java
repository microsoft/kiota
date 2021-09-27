package com.microsoft.kiota.authentication;

import com.microsoft.kiota.RequestInformation;

import java.util.concurrent.CompletableFuture;

import javax.annotation.Nonnull;

/** Authenticates the application request. */
public interface AuthenticationProvider {
    /**
     * Authenticates the application request.
     * @param request the request to authenticate.
     * @return a CompletableFuture to await for the authentication to be completed.
     */
    @Nonnull
    CompletableFuture<Void> authenticateRequest(@Nonnull final RequestInformation request);
}
