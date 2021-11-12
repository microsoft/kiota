package com.microsoft.kiota;

import java.util.Collection;
import java.util.Map;
import java.util.Objects;
import java.util.concurrent.CompletableFuture;
import java.util.function.Consumer;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

/** This class can be used to wrap a request using the fluent API and get the native response object in return. */
public class NativeResponseWrapper {
    @SuppressWarnings("unchecked")
    @Nonnull
    public static <ModelType, NativeResponseType, QueryParametersType> CompletableFuture<NativeResponseType> CallAndGetNativeType(
                @Nonnull final QuadFunction<Consumer<QueryParametersType>, Consumer<Map<String,String>>, Collection<RequestOption>, ResponseHandler, CompletableFuture<ModelType>> originalCall,
                @Nullable final Consumer<QueryParametersType> q, 
                @Nullable final Consumer<Map<String,String>> h,
                @Nullable final Collection<RequestOption> o) {
        Objects.requireNonNull(originalCall, "parameter originalCall cannot be null");
        final NativeResponseHandler responseHandler = new NativeResponseHandler();
        return originalCall.apply(q, h, o, responseHandler).thenApply((val) -> {
            return (NativeResponseType)responseHandler.value;
        });
    }
    @Nonnull
    public static <ModelType, NativeResponseType, QueryParametersType> CompletableFuture<NativeResponseType> CallAndGetNativeType(
                @Nonnull final QuadFunction<Consumer<QueryParametersType>, Consumer<Map<String,String>>, Collection<RequestOption>, ResponseHandler, CompletableFuture<ModelType>> originalCall,
                @Nullable final Consumer<QueryParametersType> q,
                @Nullable final Consumer<Map<String,String>> h) {
        return CallAndGetNativeType(originalCall, q, h, null);
    }
    @Nonnull
    public static <ModelType, NativeResponseType, QueryParametersType> CompletableFuture<NativeResponseType> CallAndGetNativeType(
                @Nonnull final QuadFunction<Consumer<QueryParametersType>, Consumer<Map<String,String>>, Collection<RequestOption>, ResponseHandler, CompletableFuture<ModelType>> originalCall,
                @Nullable final Consumer<QueryParametersType> q) {
        return CallAndGetNativeType(originalCall, q, null, null);
    }
    @Nonnull
    public static <ModelType, NativeResponseType, QueryParametersType> CompletableFuture<NativeResponseType> CallAndGetNativeType(
                @Nonnull final QuadFunction<Consumer<QueryParametersType>, Consumer<Map<String,String>>, Collection<RequestOption>, ResponseHandler, CompletableFuture<ModelType>> originalCall) {
        return CallAndGetNativeType(originalCall, null, null, null);
    }
    @SuppressWarnings("unchecked")
    @Nonnull
    public static <ModelType, NativeResponseType, QueryParametersType, RequestBodyType> CompletableFuture<NativeResponseType> CallWithBodyAndGetNativeType(
            @Nonnull final PentaFunction<RequestBodyType, Consumer<QueryParametersType>, Consumer<Map<String, String>>, Collection<RequestOption>, ResponseHandler, CompletableFuture<ModelType>> originalCall,
            @Nonnull final RequestBodyType requestBody,
            @Nullable final Consumer<QueryParametersType> q, 
            @Nullable final Consumer<Map<String,String>> h,
            @Nullable final Collection<RequestOption> o) {
        Objects.requireNonNull(originalCall, "parameter originalCall cannot be null");
        Objects.requireNonNull(requestBody, "parameter requestBody cannot be null");
        final NativeResponseHandler responseHandler = new NativeResponseHandler();
        return originalCall.apply(requestBody, q, h, o, responseHandler).thenApply((val) -> {
            return (NativeResponseType)responseHandler.value;
        });
    }
    @Nonnull
    public static <ModelType, NativeResponseType, QueryParametersType, RequestBodyType> CompletableFuture<NativeResponseType> CallWithBodyAndGetNativeType(
            @Nonnull final PentaFunction<RequestBodyType, Consumer<QueryParametersType>, Consumer<Map<String, String>>, Collection<RequestOption>, ResponseHandler, CompletableFuture<ModelType>> originalCall,
            @Nonnull final RequestBodyType requestBody,
            @Nullable final Consumer<QueryParametersType> q,
            @Nullable final Consumer<Map<String,String>> h) {
        return CallWithBodyAndGetNativeType(originalCall, requestBody, q, h, null);
    }
    @Nonnull
    public static <ModelType, NativeResponseType, QueryParametersType, RequestBodyType> CompletableFuture<NativeResponseType> CallWithBodyAndGetNativeType(
            @Nonnull final PentaFunction<RequestBodyType, Consumer<QueryParametersType>, Consumer<Map<String, String>>, Collection<RequestOption>, ResponseHandler, CompletableFuture<ModelType>> originalCall,
            @Nonnull final RequestBodyType requestBody,
            @Nullable final Consumer<QueryParametersType> q) {
        return CallWithBodyAndGetNativeType(originalCall, requestBody, q, null, null);
    }
    @Nonnull
    public static <ModelType, NativeResponseType, QueryParametersType, RequestBodyType> CompletableFuture<NativeResponseType> CallWithBodyAndGetNativeType(
            @Nonnull final PentaFunction<RequestBodyType, Consumer<QueryParametersType>, Consumer<Map<String, String>>, Collection<RequestOption>, ResponseHandler, CompletableFuture<ModelType>> originalCall,
            @Nonnull final RequestBodyType requestBody) {
        return CallWithBodyAndGetNativeType(originalCall, requestBody, null, null, null);
    }
}
