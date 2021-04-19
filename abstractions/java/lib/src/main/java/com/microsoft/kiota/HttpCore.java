package com.microsoft.kiota;

import java.util.concurrent.CompletableFuture;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

import com.microsoft.kiota.serialization.Parsable;

public interface HttpCore {
    <ModelType extends Parsable> CompletableFuture<ModelType> sendAsync(@Nonnull final RequestInfo requestInfo, @Nonnull final Class<ModelType> targetClass, @Nullable final ResponseHandler responseHandler);
}