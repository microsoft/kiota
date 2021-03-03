package com.microsoft.kiota;

import java.util.concurrent.CompletableFuture;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

public class NativeResponseHandler implements ResponseHandler {
    @Nullable
    public Object value;

    @Nonnull
    @Override
    public <NativeResponseType, ModelType> CompletableFuture<ModelType> handleResponseAsync(
            NativeResponseType response) {
        this.value = response;
        return CompletableFuture.completedFuture(null);
    }
    
}
