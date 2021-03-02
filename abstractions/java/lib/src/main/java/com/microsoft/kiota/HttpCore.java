package com.microsoft.kiota;

import java.util.concurrent.CompletableFuture;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

public interface HttpCore {
    <NativeResponseType, ModelType> CompletableFuture<ModelType> sendAsync(@Nonnull final RequestInfo requestInfo, @Nullable final ResponseHandler responseHandler);
}