package com.microsoft.kiota;

import java.io.InputStream;
import java.util.concurrent.CompletableFuture;

import javax.annotation.Nonnull;

public interface HttpCore {
    CompletableFuture<InputStream> sendAsync(@Nonnull RequestInfo requestInfo);
    <NativeResponseType> CompletableFuture<NativeResponseType> sendNativeAsync(@Nonnull RequestInfo requestInfo);
}