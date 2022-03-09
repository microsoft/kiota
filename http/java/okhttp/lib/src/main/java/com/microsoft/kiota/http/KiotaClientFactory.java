package com.microsoft.kiota.http;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

import com.microsoft.kiota.http.middleware.RedirectHandler;
import com.microsoft.kiota.http.middleware.RetryHandler;
import com.microsoft.kiota.http.middleware.TelemetryHandler;

import okhttp3.Interceptor;
import okhttp3.OkHttpClient;

/** This class is used to build the HttpClient instance used by the core service. */
public class KiotaClientFactory {
    private KiotaClientFactory() { }
    /**
     * Creates an OkHttpClient Builder with the default configuration and middlewares.
     * @return an OkHttpClient Builder instance.
     */
    @Nonnull
    public static OkHttpClient.Builder Create() {
        return Create(null);
    }
    /**
     * Creates an OkHttpClient Builder with the default configuration and middlewares.
     * @param interceptors The interceptors to add to the client. Will default to CreateDefaultInterceptors() if null.
     * @return an OkHttpClient Builder instance.
     */
    @Nonnull
    public static OkHttpClient.Builder Create(@Nullable final Interceptor[] interceptors) {
        final OkHttpClient.Builder builder = new OkHttpClient.Builder(); //TODO configure the default client options.
        final Interceptor[] interceptorsOrDefault = interceptors != null ? interceptors : CreateDefaultInterceptors();
        for (final Interceptor interceptor : interceptorsOrDefault) {
            builder.addInterceptor(interceptor);
        }
        return builder; 
    }
    /**
     * Creates the default interceptors for the client.
     * @return an array of interceptors.
     */
    @Nonnull
    public static Interceptor[] CreateDefaultInterceptors() {
        return new Interceptor[] {
            new RedirectHandler(),
            new RetryHandler(),
            new TelemetryHandler()
        }; //TODO add the list of default interceptors when they are ready
        //DO we want to add Telemetry and Chaos as defaults? 
    }
}