package com.microsoft.kiota;

import java.util.concurrent.CompletableFuture;

import javax.annotation.Nonnull;

/** Defines the contract for a response handler. */
public interface ResponseHandler {
    /**
     * Callback method that is invoked when a response is received.
     * @param response The native response object.
     * @param <NativeResponseType> The type of the native response object.
     * @param <ModelType> The type of the response model object.
     * @return A CompletableFuture that represents the asynchronous operation and contains the deserialized response.
     */
    @Nonnull
    <NativeResponseType, ModelType> CompletableFuture<ModelType> handleResponseAsync(@Nonnull final NativeResponseType response);
}