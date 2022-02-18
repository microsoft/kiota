package com.microsoft.kiota;

import java.util.HashMap;
import java.util.concurrent.CompletableFuture;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

import com.microsoft.kiota.serialization.Parsable;

/** Default response handler to access the native response object. */
public class NativeResponseHandler implements ResponseHandler {
    /** Native response object as returned by the core service */
    @Nullable
    public Object value;

    /** The error mappings for the response to use when deserializing failed responses bodies. Where an error code like 401 applies specifically to that status code, a class code like 4XX applies to all status codes within the range if an the specific error code is not present. */
    @Nullable
    public HashMap<String, Class<? extends Parsable>> errorMappings;

    /** {@inheritdoc} */
    @Nonnull
    @Override
    public <NativeResponseType, ModelType> CompletableFuture<ModelType> handleResponseAsync(
            NativeResponseType response,
            HashMap<String, Class<? extends Parsable>> errorMappings) {
        this.value = response;
        this.errorMappings = errorMappings;
        return CompletableFuture.completedFuture(null);
    }
    
}
