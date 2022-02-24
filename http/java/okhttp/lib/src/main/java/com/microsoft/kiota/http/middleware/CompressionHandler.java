package com.microsoft.kiota.http.middleware;

import java.io.IOException;

import okhttp3.Interceptor;
import okhttp3.Response;

public class CompressionHandler implements Interceptor {

    private final String GZip = "gzip";

    @Override
    public Response intercept(Chain arg0) throws IOException {
        // TODO Auto-generated method stub
        return null;
    }
    
}
