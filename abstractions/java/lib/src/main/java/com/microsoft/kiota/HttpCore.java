package com.microsoft.kiota;

import java.io.InputStream;
import java.util.concurrent.CompletableFuture;

import javax.annotation.Nonnull;

public interface HttpCore<NativeResponseType> {
    CompletableFuture<InputStream> sendAsync(@Nonnull RequestInfo requestInfo);
    CompletableFuture<NativeResponseType> sendNativeAsync(@Nonnull RequestInfo requestInfo);
}