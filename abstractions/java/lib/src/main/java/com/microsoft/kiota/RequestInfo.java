package com.microsoft.kiota;

import java.net.URI;
import java.io.InputStream;
import java.util.HashMap;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

public class RequestInfo {
    @Nullable
    public URI uri;
    @Nullable
    public HttpMethod httpMethod;
    @Nonnull
    public HashMap<String, Object> queryParameters = new HashMap<>(); //TODO case insensitive
    @Nonnull
    public HashMap<String, String> headers = new HashMap<>(); // TODO case insensitive
    @Nullable
    public InputStream Content;
}
