package com.microsoft.kiota;

import java.util.concurrent.CompletableFuture;

import javax.annotation.Nonnull;

public interface ResponseHandler {
    @Nonnull
    <NativeResponseType, ModelType> CompletableFuture<ModelType> handleResponseAsync(@Nonnull final NativeResponseType response);
}