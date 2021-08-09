package com.microsoft.kiota.http;

import com.microsoft.kiota.AuthenticationProvider;

import okhttp3.OkHttpClient;

import javax.annotation.Nullable;

/** This class is used to build the HttpClient instance used by the core service. */
public class OkHttpClientBuilder {
    private OkHttpClientBuilder() { }
    /**
     * Creates an OkHttpClient Builder with the default configuration and middlewares including a authentention middleware using the {@link AuthenticationProvider} if provided.
     * @param authenticationProvider the authentication provider used to authenticate the requests.
     * @return an OkHttpClient Builder instance.
     */
    public static OkHttpClient.Builder Create(@Nullable final AuthenticationProvider authenticationProvider) {
        return new OkHttpClient.Builder(); //TODO configure the default client options.
        //TODO add the default middlewares when they are ready
    }
}