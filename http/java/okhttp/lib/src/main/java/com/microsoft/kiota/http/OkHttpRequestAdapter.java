package com.microsoft.kiota.http;

import java.io.IOException;
import java.io.InputStream;
import java.net.MalformedURLException;
import java.net.URISyntaxException;
import java.time.OffsetDateTime;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
import java.util.StringJoiner;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

import com.microsoft.kiota.ApiClientBuilder;
import com.microsoft.kiota.ApiException;
import com.microsoft.kiota.RequestInformation;
import com.microsoft.kiota.RequestOption;
import com.microsoft.kiota.ResponseHandler;
import com.microsoft.kiota.authentication.AuthenticationProvider;
import com.microsoft.kiota.serialization.ParseNodeFactoryRegistry;
import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.ParseNodeFactory;
import com.microsoft.kiota.serialization.SerializationWriterFactory;
import com.microsoft.kiota.serialization.SerializationWriterFactoryRegistry;
import com.microsoft.kiota.store.BackingStoreFactory;
import com.microsoft.kiota.store.BackingStoreFactorySingleton;

import okhttp3.MediaType;
import okhttp3.OkHttpClient;
import okhttp3.Request;
import okhttp3.RequestBody;
import okhttp3.ResponseBody;
import okhttp3.Response;
import okio.BufferedSink;

public class OkHttpRequestAdapter implements com.microsoft.kiota.RequestAdapter {
    private final static String contentTypeHeaderKey = "Content-Type";
    private final OkHttpClient client;
    private final AuthenticationProvider authProvider;
    private ParseNodeFactory pNodeFactory;
    private SerializationWriterFactory sWriterFactory;
    private String baseUrl = "";
    public void setBaseUrl(@Nonnull final String baseUrl) {
        this.baseUrl = Objects.requireNonNull(baseUrl);
    }
    @Nonnull
    public String getBaseUrl() {
        return baseUrl;
    }
    public OkHttpRequestAdapter(@Nonnull final AuthenticationProvider authenticationProvider){
        this(authenticationProvider, null, null, null);
    }
    public OkHttpRequestAdapter(@Nonnull final AuthenticationProvider authenticationProvider, @Nonnull final ParseNodeFactory parseNodeFactory) {
        this(authenticationProvider, parseNodeFactory, null, null);
        Objects.requireNonNull(parseNodeFactory, "parameter parseNodeFactory cannot be null");
    }
    public OkHttpRequestAdapter(@Nonnull final AuthenticationProvider authenticationProvider, @Nonnull final ParseNodeFactory parseNodeFactory, @Nullable final SerializationWriterFactory serializationWriterFactory) {
        this(authenticationProvider, parseNodeFactory, serializationWriterFactory, null);
        Objects.requireNonNull(serializationWriterFactory, "parameter serializationWriterFactory cannot be null");
    }
    public OkHttpRequestAdapter(@Nonnull final AuthenticationProvider authenticationProvider, @Nullable final ParseNodeFactory parseNodeFactory, @Nullable final SerializationWriterFactory serializationWriterFactory, @Nullable final OkHttpClient client) {
        this.authProvider = Objects.requireNonNull(authenticationProvider, "parameter authenticationProvider cannot be null");
        if(client == null) {
            this.client = KiotaClientFactory.Create().build();
        } else {
            this.client = client;
        }
        if(parseNodeFactory == null) {
            pNodeFactory = ParseNodeFactoryRegistry.defaultInstance;
        } else {
            pNodeFactory = parseNodeFactory;
        }

        if(serializationWriterFactory == null) {
            sWriterFactory = SerializationWriterFactoryRegistry.defaultInstance;
        } else {
            sWriterFactory = serializationWriterFactory;
        }
    }
    public SerializationWriterFactory getSerializationWriterFactory() {
        return sWriterFactory;
    }
    public void enableBackingStore(@Nullable final BackingStoreFactory backingStoreFactory) {
        this.pNodeFactory = Objects.requireNonNull(ApiClientBuilder.enableBackingStoreForParseNodeFactory(pNodeFactory));
        this.sWriterFactory = Objects.requireNonNull(ApiClientBuilder.enableBackingStoreForSerializationWriterFactory(sWriterFactory));
        if(backingStoreFactory != null) {
            BackingStoreFactorySingleton.instance = backingStoreFactory;
        }
    }
    @Nonnull
    public <ModelType extends Parsable> CompletableFuture<Iterable<ModelType>> sendCollectionAsync(@Nonnull final RequestInformation requestInfo, @Nonnull final Class<ModelType> targetClass, @Nullable final ResponseHandler responseHandler, @Nullable final HashMap<String, Class<Parsable>> errorMappings) {
        Objects.requireNonNull(requestInfo, "parameter requestInfo cannot be null");

        return this.getHttpResponseMessage(requestInfo)
        .thenCompose(r -> this.throwFailedResponse(r, errorMappings))
        .thenCompose(response -> {
            if(responseHandler == null) {
                try {
                    final ParseNode rootNode = getRootParseNode(response);
                    final Iterable<ModelType> result = rootNode.getCollectionOfObjectValues(targetClass);
                    return CompletableFuture.completedStage(result);
                } catch(IOException ex) {
                    return CompletableFuture.failedFuture(new RuntimeException("failed to read the response body", ex));
                } finally {
                    response.close();
                }
            } else {
                return responseHandler.handleResponseAsync(response, errorMappings);
            }
        });
    }
    @Nonnull
    public <ModelType extends Parsable> CompletableFuture<ModelType> sendAsync(@Nonnull final RequestInformation requestInfo, @Nonnull final Class<ModelType> targetClass, @Nullable final ResponseHandler responseHandler, @Nullable final HashMap<String, Class<Parsable>> errorMappings) {
        Objects.requireNonNull(requestInfo, "parameter requestInfo cannot be null");

        return this.getHttpResponseMessage(requestInfo)
        .thenCompose(r -> this.throwFailedResponse(r, errorMappings))
        .thenCompose(response -> {
            if(responseHandler == null) {
                try {
                    final ParseNode rootNode = getRootParseNode(response);
                    final ModelType result = rootNode.getObjectValue(targetClass);
                    return CompletableFuture.completedStage(result);
                } catch(IOException ex) {
                    return CompletableFuture.failedFuture(new RuntimeException("failed to read the response body", ex));
                } finally {
                    response.close();
                }
            } else {
                return responseHandler.handleResponseAsync(response, errorMappings);
            }
        });
    }
    private String getMediaTypeAndSubType(final MediaType mediaType) {
        return mediaType.type() + "/" + mediaType.subtype();
    }
    @Nonnull
    public <ModelType> CompletableFuture<ModelType> sendPrimitiveAsync(@Nonnull final RequestInformation requestInfo, @Nonnull final Class<ModelType> targetClass, @Nullable final ResponseHandler responseHandler, @Nullable final HashMap<String, Class<Parsable>> errorMappings) {
        return this.getHttpResponseMessage(requestInfo)
        .thenCompose(r -> this.throwFailedResponse(r, errorMappings))
        .thenCompose(response -> {
            if(responseHandler == null) {
                try {
                    if(targetClass == Void.class) {
                        return CompletableFuture.completedStage(null);
                    } else {
                        if(targetClass == InputStream.class) {
                            final ResponseBody body = response.body();
                            final InputStream rawInputStream = body.byteStream();
                            return CompletableFuture.completedStage((ModelType)rawInputStream);
                        }
                        final ParseNode rootNode = getRootParseNode(response);
                        Object result;
                        if(targetClass == Boolean.class) {
                            result = rootNode.getBooleanValue();
                        } else if(targetClass == String.class) {
                            result = rootNode.getStringValue();
                        } else if(targetClass == Integer.class) {
                            result = rootNode.getIntegerValue();
                        } else if(targetClass == Float.class) {
                            result = rootNode.getFloatValue();
                        } else if(targetClass == Long.class) {
                            result = rootNode.getLongValue();
                        } else if(targetClass == UUID.class) {
                            result = rootNode.getUUIDValue();
                        } else if(targetClass == OffsetDateTime.class) {
                            result = rootNode.getOffsetDateTimeValue();
                        } else {
                            throw new RuntimeException("unexpected payload type " + targetClass.getName());
                        }
                        return CompletableFuture.completedStage((ModelType)result);
                    }
                } catch(IOException ex) {
                    return CompletableFuture.failedFuture(new RuntimeException("failed to read the response body", ex));
                } finally {
                    response.close();
                }
            } else {
                return responseHandler.handleResponseAsync(response, errorMappings);
            }
        });
    }
    public <ModelType> CompletableFuture<Iterable<ModelType>> sendPrimitiveCollectionAsync(@Nonnull final RequestInformation requestInfo, @Nonnull final Class<ModelType> targetClass, @Nullable final ResponseHandler responseHandler, @Nullable final HashMap<String, Class<Parsable>> errorMappings) {
        Objects.requireNonNull(requestInfo, "parameter requestInfo cannot be null");

        return this.getHttpResponseMessage(requestInfo)
        .thenCompose(r -> this.throwFailedResponse(r, errorMappings))
        .thenCompose(response -> {
            if(responseHandler == null) {
                try {
                    final ParseNode rootNode = getRootParseNode(response);
                    final Iterable<ModelType> result = rootNode.getCollectionOfPrimitiveValues(targetClass);
                    return CompletableFuture.completedStage(result);
                } catch(IOException ex) {
                    return CompletableFuture.failedFuture(new RuntimeException("failed to read the response body", ex));
                } finally {
                    response.close();
                }
            } else {
                return responseHandler.handleResponseAsync(response, errorMappings);
            }
        });
    }
    private ParseNode getRootParseNode(final Response response) throws IOException {
        final ResponseBody body = response.body();
        try (final InputStream rawInputStream = body.byteStream()) {
            final ParseNode rootNode = pNodeFactory.getParseNode(getMediaTypeAndSubType(body.contentType()), rawInputStream);
            return rootNode;
        }
    }
    private CompletableFuture<Response> throwFailedResponse(final Response response, final HashMap<String, Class<Parsable>> errorMappings) {
        if (response.isSuccessful()) return CompletableFuture.completedFuture(response);

        final String statusCodeAsString = Integer.toString(response.code());
        final Integer statusCode = response.code();
        if (errorMappings == null ||
           !errorMappings.containsKey(statusCodeAsString) &&
           !(statusCode >= 400 && statusCode < 500 && errorMappings.containsKey("4XX")) &&
           !(statusCode >= 500 && statusCode < 600 && errorMappings.containsKey("5XX"))) {
            return CompletableFuture.failedFuture(new ApiException("the server returned an unexpected status code and no error class is registered for this code " + statusCode));
        }
        final Class<Parsable> errorClass = errorMappings.containsKey(statusCodeAsString) ?
                                                    errorMappings.get(statusCodeAsString) :
                                                    (statusCode >= 400 && statusCode < 500 ?
                                                        errorMappings.get("4XX") :
                                                        errorMappings.get("5XX"));
        try {
            final ParseNode rootNode = getRootParseNode(response);
            final Parsable error = rootNode.getObjectValue(errorClass);
            if (error instanceof Exception) {
                return CompletableFuture.failedFuture((Exception)error);
            } else {
                return CompletableFuture.failedFuture(new ApiException("unexpected error type " + error.getClass().getName()));
            }
        } catch (IOException ex) {
            return CompletableFuture.failedFuture(ex);
        } finally {
            response.close();
        }
    }
    private CompletableFuture<Response> getHttpResponseMessage(@Nonnull final RequestInformation requestInfo) {
        Objects.requireNonNull(requestInfo, "parameter requestInfo cannot be null");
        this.setBaseUrlForRequestInformation(requestInfo);
        return this.authProvider.authenticateRequest(requestInfo).thenCompose(x -> {
            try {
                final OkHttpCallbackFutureWrapper wrapper = new OkHttpCallbackFutureWrapper();
                this.client.newCall(getRequestFromRequestInformation(requestInfo)).enqueue(wrapper);
                return wrapper.future;
            } catch (URISyntaxException | MalformedURLException ex) {
                var result = new CompletableFuture<Response>();
                result.completeExceptionally(ex);
                return result;
            }
        });
        
    }
    private void setBaseUrlForRequestInformation(@Nonnull final RequestInformation requestInfo) {
        Objects.requireNonNull(requestInfo);
        requestInfo.pathParameters.put("baseurl", getBaseUrl());
    }
    private Request getRequestFromRequestInformation(@Nonnull final RequestInformation requestInfo) throws URISyntaxException, MalformedURLException {
        final RequestBody body = requestInfo.content == null ? null :
                                new RequestBody() {
                                    @Override
                                    public MediaType contentType() {
                                        final String contentType = requestInfo.headers.containsKey(contentTypeHeaderKey) ? requestInfo.headers.get(contentTypeHeaderKey) : "";
                                        if(contentType.isEmpty()) {
                                            return null;
                                        } else {
                                            return MediaType.parse(contentType);
                                        }
                                    }

                                    @Override
                                    public void writeTo(BufferedSink sink) throws IOException {
                                        sink.write(requestInfo.content.readAllBytes());
                                        //TODO this is dirty and is probably going to use a lot of memory for large payloads, loop on a buffer instead
                                    }

                                };
        final Request.Builder requestBuilder = new Request.Builder()
                                            .url(requestInfo.getUri().toURL())
                                            .method(requestInfo.httpMethod.toString(), body);
        for (final Map.Entry<String,String> header : requestInfo.headers.entrySet()) {
            requestBuilder.addHeader(header.getKey(), header.getValue());
        }
        for(final RequestOption option : requestInfo.getRequestOptions()) {
            requestBuilder.tag(option);
        }
        return requestBuilder.build();
    }
}
