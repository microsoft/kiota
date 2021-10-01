package com.microsoft.kiota.http;

import okhttp3.OkHttpClient;

/** This class is used to build the HttpClient instance used by the core service. */
public class KiotaClientFactory {
    private KiotaClientFactory() { }
    /**
     * Creates an OkHttpClient Builder with the default configuration and middlewares.
     * @return an OkHttpClient Builder instance.
     */
    public static OkHttpClient.Builder Create() {
        return new OkHttpClient.Builder(); //TODO configure the default client options.
        //TODO add the default middlewares when they are ready
    }
}