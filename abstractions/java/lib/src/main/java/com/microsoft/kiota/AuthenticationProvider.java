package com.microsoft.kiota;

import java.net.URI;
import java.util.concurrent.CompletableFuture;

import javax.annotation.Nonnull;

public interface AuthenticationProvider {
    CompletableFuture<String> getAuthorizationToken(@Nonnull final URI requestUri);
}
