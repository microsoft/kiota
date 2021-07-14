package com.microsoft.kiota;

import java.net.URI;
import java.util.concurrent.CompletableFuture;

import javax.annotation.Nonnull;

/** Authenticates the application and returns a token. */
public interface AuthenticationProvider {
    /**
     * Authenticates the application and returns a token base on the provided Uri.
     * @param requestUri the Uri to authenticate the request for.
     * @return a CompletableFuture that will be completed with the token or null if the target request Uri doesn't correspond to a valid resource.
     */
    CompletableFuture<String> getAuthorizationToken(@Nonnull final URI requestUri);
}
