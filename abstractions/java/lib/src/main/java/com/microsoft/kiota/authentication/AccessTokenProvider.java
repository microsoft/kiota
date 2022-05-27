package com.microsoft.kiota.authentication;

import java.net.URI;
import java.util.Map;
import java.util.concurrent.CompletableFuture;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

/** Returns access tokens */
public interface AccessTokenProvider {
    /**
     * This method returns the access token for the provided url.
     * @param uri The target URI to get an access token for.
     * @param additionalAuthenticationContext Additional authentication context to pass to the authentication library.
     * @return A CompletableFuture that holds the access token.
     */
    @Nonnull
    CompletableFuture<String> getAuthorizationToken(@Nonnull final URI uri, @Nullable final Map<String, Object> additionalAuthenticationContext);
    /**
     * Returns the allowed hosts validator.
     * @return The allowed hosts validator.
     */
    @Nonnull
    AllowedHostsValidator getAllowedHostsValidator();
}