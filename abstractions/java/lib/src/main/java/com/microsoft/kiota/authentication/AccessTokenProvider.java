package com.microsoft.kiota.authentication;

import java.net.URI;
import java.util.concurrent.CompletableFuture;

import javax.annotation.Nonnull;

/** Returns access tokens */
public interface AccessTokenProvider {
    /**
     * This method returns the access token for the provided url.
     * @param uri The target URI to get an access token for.
     * @return A CompletableFuture that holds the access token.
     */
    @Nonnull
    CompletableFuture<String> getAuthorizationToken(@Nonnull final URI uri);
}