package com.microsoft.kiota;

import java.util.Map;
import java.util.Objects;
import java.util.concurrent.CompletableFuture;
import java.util.function.Consumer;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

public class NativeResponseWrapper {
    @SuppressWarnings("unchecked")
    @Nonnull
    public static <ModelType, NativeResponseType, QueryParametersType> CompletableFuture<NativeResponseType> CallAndGetNativeType(
                @Nonnull final TriFunction<Consumer<QueryParametersType>, Consumer<Map<String,String>>, ResponseHandler, CompletableFuture<ModelType>> originalCall,
                @Nullable final Consumer<QueryParametersType> q, 
                @Nullable final Consumer<Map<String,String>> h) {
        Objects.requireNonNull(originalCall, "parameter originalCall cannot be null");
        final NativeResponseHandler responseHandler = new NativeResponseHandler();
        return originalCall.apply(q, h, responseHandler).thenApply((val) -> {
            return (NativeResponseType)responseHandler.value;
        });
    }
    @Nonnull
    public static <ModelType, NativeResponseType, QueryParametersType> CompletableFuture<NativeResponseType> CallAndGetNativeType(
                @Nonnull final TriFunction<Consumer<QueryParametersType>, Consumer<Map<String,String>>, ResponseHandler, CompletableFuture<ModelType>> originalCall,
                @Nullable final Consumer<QueryParametersType> q) {
        return CallAndGetNativeType(originalCall, q, null);
    }
    @Nonnull
    public static <ModelType, NativeResponseType, QueryParametersType> CompletableFuture<NativeResponseType> CallAndGetNativeType(
                @Nonnull final TriFunction<Consumer<QueryParametersType>, Consumer<Map<String,String>>, ResponseHandler, CompletableFuture<ModelType>> originalCall) {
        return CallAndGetNativeType(originalCall, null);
    }
    @SuppressWarnings("unchecked")
    @Nonnull
    public static <ModelType, NativeResponseType, QueryParametersType, RequestBodyType> CompletableFuture<NativeResponseType> CallWithBodyAndGetNativeType(
            @Nonnull final QuadFunction<RequestBodyType, Consumer<QueryParametersType>, Consumer<Map<String, String>>, ResponseHandler, CompletableFuture<ModelType>> originalCall,
            @Nonnull final RequestBodyType requestBody,
            @Nullable final Consumer<QueryParametersType> q, 
            @Nullable final Consumer<Map<String,String>> h) {
        Objects.requireNonNull(originalCall, "parameter originalCall cannot be null");
        Objects.requireNonNull(requestBody, "parameter requestBody cannot be null");
        final NativeResponseHandler responseHandler = new NativeResponseHandler();
        return originalCall.apply(requestBody, q, h, responseHandler).thenApply((val) -> {
            return (NativeResponseType)responseHandler.value;
        });
    }
    @Nonnull
    public static <ModelType, NativeResponseType, QueryParametersType, RequestBodyType> CompletableFuture<NativeResponseType> CallWithBodyAndGetNativeType(
            @Nonnull final QuadFunction<RequestBodyType, Consumer<QueryParametersType>, Consumer<Map<String, String>>, ResponseHandler, CompletableFuture<ModelType>> originalCall,
            @Nonnull final RequestBodyType requestBody,
            @Nullable final Consumer<QueryParametersType> q) {
        return CallWithBodyAndGetNativeType(originalCall, requestBody, q, null);
    }
    @Nonnull
    public static <ModelType, NativeResponseType, QueryParametersType, RequestBodyType> CompletableFuture<NativeResponseType> CallWithBodyAndGetNativeType(
            @Nonnull final QuadFunction<RequestBodyType, Consumer<QueryParametersType>, Consumer<Map<String, String>>, ResponseHandler, CompletableFuture<ModelType>> originalCall,
            @Nonnull final RequestBodyType requestBody) {
        return CallWithBodyAndGetNativeType(originalCall, requestBody, null);
    }
}
