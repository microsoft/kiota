package com.microsoft.kiota.http.middleware;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.mockito.Mockito.mock;

import java.io.IOException;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.net.HttpURLConnection;
import java.net.URI;
import java.net.URISyntaxException;

import com.microsoft.kiota.HttpMethod;
import com.microsoft.kiota.RequestInformation;
import com.microsoft.kiota.authentication.AuthenticationProvider;
import com.microsoft.kiota.http.KiotaClientFactory;
import com.microsoft.kiota.http.OkHttpRequestAdapter;
import com.microsoft.kiota.http.middleware.RetryHandler;
import com.microsoft.kiota.http.middleware.options.RetryHandlerOption;

import org.junit.jupiter.api.Test;
import org.mockito.InjectMocks;

import okhttp3.Interceptor;
import okhttp3.OkHttpClient;
import okhttp3.Protocol;
import okhttp3.Request;
import okhttp3.Response;

public class RetryHandlerTests {
   
    @InjectMocks
    public OkHttpRequestAdapter adapter = new OkHttpRequestAdapter(mock(AuthenticationProvider.class));

    @Test 
    public void RetryHandlerConstructorDefaults() {
        RetryHandler retryHandler = new RetryHandler();
        RetryHandlerOption retryHandlerOption = new RetryHandlerOption();
        
        assertEquals(retryHandler.getRetryOptions().delay(), retryHandlerOption.delay());
        assertEquals(retryHandler.getRetryOptions().maxRetries(), retryHandlerOption.maxRetries());
        assertEquals(retryHandler.getRetryOptions().shouldRetry(), retryHandlerOption.shouldRetry());
    }

    @Test
    public void RetryHandlerRetryOptionConstructor() throws SecurityException, IllegalArgumentException {
        RetryHandlerOption retryHandlerOption = new RetryHandlerOption();
        RetryHandler retryHandler = new RetryHandler(retryHandlerOption);

        Request request = new Request.Builder().url("https://graph.microsoft.com/v1.0/me").build();
        Response response = new Response.Builder()
                .protocol(Protocol.HTTP_1_1)
                .code(HttpURLConnection.HTTP_GATEWAY_TIMEOUT)
                .message("Gateway Timeout")
                .request(request).build();

        assertTrue(retryHandler.retryRequest(response, 1, request, retryHandlerOption));
    }



}
