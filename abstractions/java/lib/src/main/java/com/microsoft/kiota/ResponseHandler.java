package com.microsoft.kiota;

import java.util.HashMap;
import java.util.concurrent.CompletableFuture;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParsableFactory;

/** Defines the contract for a response handler. */
public interface ResponseHandler {
    /**
     * Callback method that is invoked when a response is received.
     * @param response The native response object.
     * @param errorMappings the error mappings for the response to use when deserializing failed responses bodies. Where an error code like 401 applies specifically to that status code, a class code like 4XX applies to all status codes within the range if an the specific error code is not present.
     * @param <NativeResponseType> The type of the native response object.
     * @param <ModelType> The type of the response model object.
     * @return A CompletableFuture that represents the asynchronous operation and contains the deserialized response.
     */
    @Nonnull
    <NativeResponseType, ModelType> CompletableFuture<ModelType> handleResponseAsync(@Nonnull final NativeResponseType response, @Nullable final HashMap<String, ParsableFactory<? extends Parsable>> errorMappings);
}