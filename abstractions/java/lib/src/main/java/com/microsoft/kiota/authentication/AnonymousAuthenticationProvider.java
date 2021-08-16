package com.microsoft.kiota.authentication;

import com.microsoft.kiota.RequestInfo;

import java.util.concurrent.CompletableFuture;

/** This authentication provider does not perform any authentication. */
public class AnonymousAuthenticationProvider implements AuthenticationProvider {
    public CompletableFuture<Void> authenticateRequest(final RequestInfo request) {
        return CompletableFuture.completedFuture(null);
    }    
}