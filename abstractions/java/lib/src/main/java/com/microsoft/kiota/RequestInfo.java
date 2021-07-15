package com.microsoft.kiota;

import java.net.URI;
import java.io.IOException;
import java.io.InputStream;
import java.util.Collection;
import java.util.HashMap;
import java.util.Objects;
import java.util.function.Function;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.SerializationWriter;
import com.microsoft.kiota.HttpCore;

/** This class represents an abstract HTTP request. */
public class RequestInfo {
    /** The URI of the request. */
    @Nullable
    public URI uri;
    /** The HTTP method for the request */
    @Nullable
    public HttpMethod httpMethod;
    /** The Query Parameters of the request. */
    @Nonnull
    public HashMap<String, Object> queryParameters = new HashMap<>(); //TODO case insensitive
    /** The Request Headers. */
    @Nonnull
    public HashMap<String, String> headers = new HashMap<>(); // TODO case insensitive
    /** The Request Body. */
    @Nullable
    public InputStream content;
    private HashMap<String, MiddlewareOption> _middlewareOptions = new HashMap<>();
    /**
     * Gets the middleware options for this request. Options are unique by type. If an option of the same type is added twice, the last one wins.
     * @return the middleware options for this request.
     */
    public Collection<MiddlewareOption> getMiddlewareOptions() { return _middlewareOptions.values(); }
    /**
     * Adds a middleware option to this request.
     * @param option the middleware option to add.
     */
    public void AddMiddlewareOptions(@Nullable final MiddlewareOption... options) { 
        if(options == null || options.length == 0) return;
        for(final MiddlewareOption option : options) {
            _middlewareOptions.put(option.getClass().getCanonicalName(), option);
        }
    }
    /**
     * Removes a middleware option from this request.
     * @param option the middleware option to remove.
     */
    public void RemoveMiddlewareOptions(@Nullable final MiddlewareOption... options) {
        if(options == null || options.length == 0) return;
        for(final MiddlewareOption option : options) {
            _middlewareOptions.remove(option.getClass().getCanonicalName());
        }
    }
    private static String binaryContentType = "application/octet-stream";
    private static String contentTypeHeader = "Content-Type";
    /**
     * Sets the request body to be a binary stream.
     * @param value the binary stream
     */
    public void setStreamContent(@Nonnull final InputStream value) {
        Objects.requireNonNull(value);
        this.content = value;
        headers.put(contentTypeHeader, binaryContentType);
    }
    /**
     * Sets the request body from a model with the specified content type.
     * @param value the model.
     * @param contentType the content type.
     * @param httpCore The core service to get the serialization writer from.
     * @param <T> the model type.
     */
    public <T extends Parsable> void setContentFromParsable(@Nonnull final T value, @Nonnull final HttpCore httpCore, @Nonnull final String contentType) {
        Objects.requireNonNull(httpCore);
        Objects.requireNonNull(value);
        Objects.requireNonNull(contentType);
        try(final SerializationWriter writer = httpCore.getSerializationWriterFactory().getSerializationWriter(contentType)) {
            headers.put(contentTypeHeader, contentType);
            writer.writeObjectValue(null, value);
            this.content = writer.getSerializedContent();
        } catch (IOException ex) {
            throw new RuntimeException("could not serialize payload", ex);
        }
    }
}
