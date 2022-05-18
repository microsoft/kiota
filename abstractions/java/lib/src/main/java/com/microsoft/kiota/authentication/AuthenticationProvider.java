package com.microsoft.kiota.authentication;

import com.microsoft.kiota.RequestInformation;

import java.util.Map;
import java.util.concurrent.CompletableFuture;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

/** Authenticates the application request. */
public interface AuthenticationProvider {
    /**
     * Authenticates the application request.
     * @param request the request to authenticate.
     * @param additionalAuthenticationContext Additional authentication context to pass to the authentication library.
     * @return a CompletableFuture to await for the authentication to be completed.
     */
    @Nonnull
    CompletableFuture<Void> authenticateRequest(@Nonnull final RequestInformation request, @Nullable final Map<String, Object> additionalAuthenticationContext);
}
