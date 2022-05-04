package com.microsoft.kiota;

import java.net.URI;
import java.net.URISyntaxException;
import java.io.IOException;
import java.io.InputStream;
import java.lang.reflect.Field;
import java.util.Arrays;
import java.util.Collection;
import java.util.HashMap;
import java.util.Map;
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

    private HashMap<String, Object> queryParameters = new HashMap<>();
    /**
     * Adds query parameters to the request based on the object passed in and its fields.
     * @param object The object to add the query parameters from.
     */
    public void addQueryParameters(@Nullable final Object parameters) {
        if (parameters == null) return;
        final Field[] fields = parameters.getClass().getFields();
        for(final Field field : fields) {
            try {
                final var value = field.get(parameters);
                var name = field.getName();
                if (field.isAnnotationPresent(QueryParameter.class)) {
                    final var annotationName = field.getAnnotation(QueryParameter.class).name();
                    if(annotationName != null && !annotationName.isEmpty()) {
                        name = annotationName;
                    }
                }
                if(value != null) {
                    if(value.getClass().isArray()) {
                        queryParameters.put(name, Arrays.asList((Object[])value));
                    } else {
                        queryParameters.put(name, value);
                    }
                }
            } catch (IllegalAccessException ex) {
                //TODO log
            }
        }
    }
    /**
     * Adds query parameters to the request.
     * @param name The name of the query parameter.
     * @param value The value to add the query parameters.
     */
    public void addQueryParameter(@Nonnull final String name, @Nullable final Object value) {
        Objects.requireNonNull(name);
        Objects.requireNonNull(value);
        queryParameters.put(name, value);
    }
    /**
     * Removes a query parameter from the request.
     * @param name The name of the query parameter to remove.
     */
    public void removeQueryParameter(@Nonnull final String name) {
        Objects.requireNonNull(name);
        queryParameters.remove(name);
    }
    /**
     * Gets the query parameters for the request.
     * @return The query parameters for the request.
     */
    @Nonnull
    @SuppressWarnings("unchecked")
    public Map<String, Object> getQueryParameters() {
        return (Map<String, Object>) queryParameters.clone();
    }
    private HashMap<String, String> headers = new HashMap<>();
    /**
     * Adds headers to the current request.
     * @param headersToAdd headers to add to the current request.
     */
    public void addRequestHeaders(@Nullable final Map<String, String> headersToAdd) {
        if (headersToAdd == null || headersToAdd.isEmpty()) return;
        headersToAdd.entrySet()
                    .stream()
                    .forEach(entry -> this.addRequestHeader(entry.getKey(), entry.getValue()));
    }
    /**
     * Adds a header to the current request.
     * @param key the key of the header to add.
     * @param value the value of the header to add.
     */
    public void addRequestHeader(@Nonnull final String key, @Nonnull final String value) {
        Objects.requireNonNull(key);
        Objects.requireNonNull(value);
        headers.put(key.toLowerCase(), value);
    }
    /**
     * Removes a request header from the current request.
     * @param key the key of the header to remove.
     */
    public void removeRequestHeader(@Nonnull final String key) {
        Objects.requireNonNull(key);
        headers.remove(key.toLowerCase());
    }
    /** 
     * Gets the request headers the for current request
     * @return the request headers for the current request.
     */
    @Nonnull
    @SuppressWarnings("unchecked")
    public Map<String, String> getRequestHeaders() {
        return (Map<String, String>) headers.clone();
    }
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
     * Adds request options to this request.
     * @param options the request options to add.
     */
    public void addRequestOptions(@Nullable final Collection<RequestOption> options) { 
        if(options == null || options.isEmpty()) return;
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
