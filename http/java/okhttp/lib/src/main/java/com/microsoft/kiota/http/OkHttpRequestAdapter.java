package com.microsoft.kiota.http;

import java.io.IOException;
import java.io.InputStream;
import java.time.OffsetDateTime;
import java.util.Map;
import java.util.Objects;
import java.util.StringJoiner;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

import com.microsoft.kiota.ApiClientBuilder;
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
import src.main.java.com.microsoft.kiota.http.KiotaClientFactory;

public class OkHttpRequestAdapter implements com.microsoft.kiota.RequestAdapter {
    private final static String contentTypeHeaderKey = "Content-Type";
    private final OkHttpClient client;
    private final AuthenticationProvider authProvider;
    private ParseNodeFactory pNodeFactory;
    private SerializationWriterFactory sWriterFactory;
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
    public <ModelType extends Parsable> CompletableFuture<Iterable<ModelType>> sendCollectionAsync(@Nonnull final RequestInformation requestInfo, @Nonnull final Class<ModelType> targetClass, @Nullable final ResponseHandler responseHandler) {
        Objects.requireNonNull(requestInfo, "parameter requestInfo cannot be null");

        return this.getHttpResponseMessage(requestInfo).thenCompose(response -> {
            if(responseHandler == null) {
                final ResponseBody body = response.body();
                try {
                    try (final InputStream rawInputStream = body.byteStream()) {
                        final ParseNode rootNode = pNodeFactory.getParseNode(getMediaTypeAndSubType(body.contentType()), rawInputStream);
                        final Iterable<ModelType> result = rootNode.getCollectionOfObjectValues(targetClass);
                        return CompletableFuture.completedStage(result);
                    }
                } catch(IOException ex) {
                    return CompletableFuture.failedFuture(new RuntimeException("failed to read the response body", ex));
                } finally {
                    response.close();
                }
            } else {
                return responseHandler.handleResponseAsync(response);
            }
        });
    }
    @Nonnull
    public <ModelType extends Parsable> CompletableFuture<ModelType> sendAsync(@Nonnull final RequestInformation requestInfo, @Nonnull final Class<ModelType> targetClass, @Nullable final ResponseHandler responseHandler) {
        Objects.requireNonNull(requestInfo, "parameter requestInfo cannot be null");

        return this.getHttpResponseMessage(requestInfo).thenCompose(response -> {
            if(responseHandler == null) {
                final ResponseBody body = response.body();
                try {
                    try (final InputStream rawInputStream = body.byteStream()) {
                        final ParseNode rootNode = pNodeFactory.getParseNode(getMediaTypeAndSubType(body.contentType()), rawInputStream);
                        final ModelType result = rootNode.getObjectValue(targetClass);
                        return CompletableFuture.completedStage(result);
                    }
                } catch(IOException ex) {
                    return CompletableFuture.failedFuture(new RuntimeException("failed to read the response body", ex));
                } finally {
                    response.close();
                }
            } else {
                return responseHandler.handleResponseAsync(response);
            }
        });
    }
    private String getMediaTypeAndSubType(final MediaType mediaType) {
        return mediaType.type() + "/" + mediaType.subtype();
    }
    @Nonnull
    public <ModelType> CompletableFuture<ModelType> sendPrimitiveAsync(@Nonnull final RequestInformation requestInfo, @Nonnull final Class<ModelType> targetClass, @Nullable final ResponseHandler responseHandler) {
        return this.getHttpResponseMessage(requestInfo).thenCompose(response -> {
            if(responseHandler == null) {
                final ResponseBody body = response.body();
                try {
                    if(targetClass == Void.class) {
                        return CompletableFuture.completedStage(null);
                    } else {
                        final InputStream rawInputStream = body.byteStream();
                        if(targetClass == InputStream.class) {
                            return CompletableFuture.completedStage((ModelType)rawInputStream);
                        }
                        final ParseNode rootNode = pNodeFactory.getParseNode(getMediaTypeAndSubType(body.contentType()), rawInputStream);
                        rawInputStream.close();
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
                return responseHandler.handleResponseAsync(response);
            }
        });
    }
    public <ModelType> CompletableFuture<Iterable<ModelType>> sendPrimitiveCollectionAsync(@Nonnull final RequestInformation requestInfo, @Nonnull final Class<ModelType> targetClass, @Nullable final ResponseHandler responseHandler) {
        Objects.requireNonNull(requestInfo, "parameter requestInfo cannot be null");

        return this.getHttpResponseMessage(requestInfo).thenCompose(response -> {
            if(responseHandler == null) {
                final ResponseBody body = response.body();
                try {
                    try (final InputStream rawInputStream = body.byteStream()) {
                        final ParseNode rootNode = pNodeFactory.getParseNode(getMediaTypeAndSubType(body.contentType()), rawInputStream);
                        final Iterable<ModelType> result = rootNode.getCollectionOfPrimitiveValues(targetClass);
                        return CompletableFuture.completedStage(result);
                    }
                } catch(IOException ex) {
                    return CompletableFuture.failedFuture(new RuntimeException("failed to read the response body", ex));
                } finally {
                    response.close();
                }
            } else {
                return responseHandler.handleResponseAsync(response);
            }
        });
    }
    private CompletableFuture<Response> getHttpResponseMessage(@Nonnull final RequestInformation requestInfo) {
        Objects.requireNonNull(requestInfo, "parameter requestInfo cannot be null");
        return this.authProvider.authenticateRequest(requestInfo).thenCompose(x -> {
            final OkHttpCallbackFutureWrapper wrapper = new OkHttpCallbackFutureWrapper();
            this.client.newCall(getRequestFromRequestInformation(requestInfo)).enqueue(wrapper);
            return wrapper.future;
        });
    }
    private Request getRequestFromRequestInformation(@Nonnull final RequestInformation requestInfo) {
        final StringBuilder urlBuilder = new StringBuilder(requestInfo.uri.toString());

        if(!requestInfo.queryParameters.isEmpty()) {
            urlBuilder.append('?');
            final StringJoiner qParamsJoiner = new StringJoiner("&");
            for (final Map.Entry<String, Object> qPram : requestInfo.queryParameters.entrySet()) {
                final Object value = qPram.getValue();
                final String valueStr = value == null ? "" : value.toString();
                qParamsJoiner.add(qPram.getKey() + (valueStr.isEmpty() ? "" : "=") + valueStr);
            }
            urlBuilder.append(qParamsJoiner.toString());
        }
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
                                            .url(urlBuilder.toString())
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
