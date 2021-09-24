package com.microsoft.kiota;

import java.util.concurrent.CompletableFuture;
import java.util.List;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.SerializationWriterFactory;
import com.microsoft.kiota.store.BackingStoreFactory;

/** Service responsible for translating abstract Request Info into concrete native HTTP requests. */
public interface HttpCore {
    /**
     * Enables the backing store proxies for the SerializationWriters and ParseNodes in use.
     * @param backingStoreFactory The backing store factory to use.
     */
    void enableBackingStore(@Nullable final BackingStoreFactory backingStoreFactory);
    /**
     * Gets the serialization writer factory currently in use for the HTTP core service.
     * @return the serialization writer factory currently in use for the HTTP core service.
     */
    @Nonnull
    SerializationWriterFactory getSerializationWriterFactory();
    /**
     * Excutes the HTTP request specified by the given RequestInformation and returns the deserialized response model.
     * @param requestInfo the request info to execute.
     * @param responseHandler The response handler to use for the HTTP request instead of the default handler.
     * @param targetClass the class of the response model to deserialize the response into.
     * @param <ModelType> the type of the response model to deserialize the response into.
     * @return a {@link CompletableFuture} with the deserialized response model.
     */
    <ModelType extends Parsable> CompletableFuture<ModelType> sendAsync(@Nonnull final RequestInformation requestInfo, @Nonnull final Class<ModelType> targetClass, @Nullable final ResponseHandler responseHandler);
    /**
     * Excutes the HTTP request specified by the given RequestInformation and returns the deserialized response model collection.
     * @param requestInfo the request info to execute.
     * @param responseHandler The response handler to use for the HTTP request instead of the default handler.
     * @param targetClass the class of the response model to deserialize the response into.
     * @param <ModelType> the type of the response model to deserialize the response into.
     * @return a {@link CompletableFuture} with the deserialized response model collection.
     */
    <ModelType extends Parsable> CompletableFuture<Iterable<ModelType>> sendCollectionAsync(@Nonnull final RequestInformation requestInfo, @Nonnull final Class<ModelType> targetClass, @Nullable final ResponseHandler responseHandler);
    /**
     * Excutes the HTTP request specified by the given RequestInformation and returns the deserialized primitive response model.
     * @param requestInfo the request info to execute.
     * @param responseHandler The response handler to use for the HTTP request instead of the default handler.
     * @param targetClass the class of the response model to deserialize the response into.
     * @param <ModelType> the type of the response model to deserialize the response into.
     * @return a {@link CompletableFuture} with the deserialized primitive response model.
     */
    <ModelType> CompletableFuture<ModelType> sendPrimitiveAsync(@Nonnull final RequestInformation requestInfo, @Nonnull final Class<ModelType> targetClass, @Nullable final ResponseHandler responseHandler);
    /**
     * Excutes the HTTP request specified by the given RequestInformation and returns the deserialized primitive collection response model.
     * @param requestInfo the request info to execute.
     * @param responseHandler The response handler to use for the HTTP request instead of the default handler.
     * @param targetClass the class of the response model to deserialize the response into.
     * @param <ModelType> the type of the response model to deserialize the response into.
     * @return a {@link CompletableFuture} with the deserialized primitive collection response model.
     */
    <ModelType> CompletableFuture<Iterable<ModelType>> sendPrimitiveCollectionAsync(@Nonnull final RequestInformation requestInfo, @Nonnull final Class<ModelType> targetClass, @Nullable final ResponseHandler responseHandler);
}