package com.microsoft.kiota.http;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

import okhttp3.Interceptor;
import okhttp3.OkHttpClient;

/** This class is used to build the HttpClient instance used by the core service. */
public class KiotaClientFactory {
    private KiotaClientFactory() { }
    /**
     * Creates an OkHttpClient Builder with the default configuration and middlewares.
     * @return an OkHttpClient Builder instance.
     */
    public static OkHttpClient.Builder Create(@Nullable final Interceptor[] interceptors) {
        final OkHttpClient.Builder builder = new OkHttpClient.Builder(); //TODO configure the default client options.
        final Interceptor[] interceptorsOrDefault = interceptors != null ? interceptors : CreateDefaultInterceptors();
        for (final Interceptor interceptor : interceptorsOrDefault) {
            builder.addInterceptor(interceptor);
        }
        return builder; 
    }
    @Nonnull
    public static Interceptor[] CreateDefaultInterceptors() {
        return new Interceptor[] {}; //TODO add the list of default interceptors when they are ready
    }
}