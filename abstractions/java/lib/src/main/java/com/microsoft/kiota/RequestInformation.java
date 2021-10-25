package com.microsoft.kiota;

import java.net.URI;
import java.net.URISyntaxException;
import java.io.IOException;
import java.io.InputStream;
import java.util.Arrays;
import java.util.Collection;
import java.util.HashMap;
import java.util.Objects;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.SerializationWriter;

import com.github.hal4j.uritemplate.URITemplate;

/** This class represents an abstract HTTP request. */
public class RequestInformation {
    /** The url template for the current request */
    public String urlTemplate;
    /** The path parameters for the current request */
    public HashMap<String, Object> pathParameters = new HashMap<>();
    private URI uri;
    /** Gets the URI of the request. 
     * @throws URISyntaxException
     */
    @Nullable
    public URI getUri() throws URISyntaxException {
        if(uri != null) {
            return uri;
        } else if(pathParameters.containsKey(RAW_URL_KEY) &&
            pathParameters.get(RAW_URL_KEY) instanceof String) {
            setUri(new URI((String)pathParameters.get(RAW_URL_KEY)));
            return uri;
        } else {
            Objects.requireNonNull(urlTemplate);
            Objects.requireNonNull(queryParameters);
            var template = new URITemplate(urlTemplate)
                            .expandOnly(new HashMap<String, Object>(queryParameters) {{
                                putAll(pathParameters);
                            }});
            return template.toURI();
        }
    }
    /** Sets the URI of the request. */
    public void setUri(@Nonnull final URI uri) {
        this.uri = Objects.requireNonNull(uri);
        if(queryParameters != null) {
            queryParameters.clear();
        }
        if(pathParameters != null) {
            pathParameters.clear();
        }
    }
    private static String RAW_URL_KEY = "request-raw-url";
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
    private HashMap<String, RequestOption> _requestOptions = new HashMap<>();
    /**
     * Gets the request options for this request. Options are unique by type. If an option of the same type is added twice, the last one wins.
     * @return the request options for this request.
     */
    public Collection<RequestOption> getRequestOptions() { return _requestOptions.values(); }
    /**
     * Adds a request option to this request.
     * @param option the request option to add.
     */
    public void addRequestOptions(@Nullable final RequestOption... options) { 
        if(options == null || options.length == 0) return;
        for(final RequestOption option : options) {
            _requestOptions.put(option.getClass().getCanonicalName(), option);
        }
    }
    /**
     * Removes a request option from this request.
     * @param option the request option to remove.
     */
    public void removeRequestOptions(@Nullable final RequestOption... options) {
        if(options == null || options.length == 0) return;
        for(final RequestOption option : options) {
            _requestOptions.remove(option.getClass().getCanonicalName());
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
     * @param values the models.
     * @param contentType the content type.
     * @param requestAdapter The adapter service to get the serialization writer from.
     * @param <T> the model type.
     */
    public <T extends Parsable> void setContentFromParsable(@Nonnull final RequestAdapter requestAdapter, @Nonnull final String contentType, @Nonnull final T... values) {
        Objects.requireNonNull(requestAdapter);
        Objects.requireNonNull(values);
        Objects.requireNonNull(contentType);
        if(values.length == 0) throw new RuntimeException("values cannot be empty");

        try(final SerializationWriter writer = requestAdapter.getSerializationWriterFactory().getSerializationWriter(contentType)) {
            headers.put(contentTypeHeader, contentType);
            if(values.length == 1) 
                writer.writeObjectValue(null, values[0]);
            else
                writer.writeCollectionOfObjectValues(null, Arrays.asList(values));
            this.content = writer.getSerializedContent();
        } catch (IOException ex) {
            throw new RuntimeException("could not serialize payload", ex);
        }
    }
}
