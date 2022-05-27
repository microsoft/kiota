package com.microsoft.kiota.http.middleware;

import java.io.IOException;
import java.util.ArrayList;
import java.util.Objects;
import java.util.AbstractMap.SimpleEntry;
import java.util.Map.Entry;

import javax.annotation.Nonnull;

import okhttp3.Interceptor;
import okhttp3.Request;
import okhttp3.Response;
/** This handlers decodes special characters in the request query parameters that had to be encoded due to RFC 6570 restrictions names before executing the request. */
public class ParametersNameDecodingHandler implements Interceptor {
    private final ParametersNameDecodingOption options;
    public ParametersNameDecodingHandler() {
        this(new ParametersNameDecodingOption());
    }
    public ParametersNameDecodingHandler(@Nonnull final ParametersNameDecodingOption options) {
        super();
        this.options = Objects.requireNonNull(options);
    }

    /**
     * {@inheritdoc}
     */
    @Override
    public Response intercept(@Nonnull final Chain chain) throws IOException {
        Objects.requireNonNull(chain);
        final Request request = chain.request();
        ParametersNameDecodingOption nameOption = request.tag(ParametersNameDecodingOption.class);
        if(nameOption == null) { nameOption = this.options; }
        final var originalUri = request.url();
        if(!originalUri.toString().contains("%") ||
            nameOption == null ||
            !nameOption.enable ||
            nameOption.parametersToDecode == null ||
            nameOption.parametersToDecode.length == 0) {
                return chain.proceed(request);
            }
        var query = originalUri.query();
        if (query == null || query.isEmpty()) {
            return chain.proceed(request);
        }
        final var symbolsToReplace = new ArrayList<SimpleEntry<String, String>>(nameOption.parametersToDecode.length);
        for (final char charToReplace : nameOption.parametersToDecode) {
            symbolsToReplace.add(new SimpleEntry<String,String>("%" + String.format("%x", (int)charToReplace), String.valueOf(charToReplace)));
        }
        for (final Entry<String, String> symbolToReplace : symbolsToReplace) {
            query = query.replace(symbolToReplace.getKey(), symbolToReplace.getValue());
        }
        final var newUrl = originalUri.newBuilder().query(query).build();
        return chain.proceed(request.newBuilder().url(newUrl).build());
    }
    
}
