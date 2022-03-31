package com.microsoft.kiota.http.middleware;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.mockito.Mockito.mock;

import java.net.HttpURLConnection;
import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.time.Instant;
import java.time.ZoneId;
import java.time.temporal.ChronoUnit;
import java.util.Date;
import java.util.Locale;
import java.util.TimeZone;

import com.microsoft.kiota.authentication.AuthenticationProvider;
import com.microsoft.kiota.http.OkHttpRequestAdapter;
import com.microsoft.kiota.http.middleware.options.IShouldRetry;
import com.microsoft.kiota.http.middleware.options.RetryHandlerOption;

import org.junit.jupiter.api.Test;
import org.mockito.InjectMocks;

import okhttp3.MediaType;
import okhttp3.Protocol;
import okhttp3.Request;
import okhttp3.RequestBody;
import okhttp3.Response;
import okio.BufferedSink;

public class RetryHandlerTests {
   
    @InjectMocks
    public OkHttpRequestAdapter adapter = new OkHttpRequestAdapter(mock(AuthenticationProvider.class));

    private final String RFC1123Pattern = "EEE, dd MMM yyyy HH:mm:ss z";
    SimpleDateFormat formatter = new SimpleDateFormat(RFC1123Pattern, Locale.ENGLISH);


    @Test 
    public void RetryHandlerConstructorDefaults() {
        RetryHandler retryHandler = new RetryHandler();
        RetryHandlerOption retryHandlerOption = new RetryHandlerOption();
        
        assertEquals(retryHandler.getRetryOptions().delay(), retryHandlerOption.delay());
        assertEquals(retryHandler.getRetryOptions().maxRetries(), retryHandlerOption.maxRetries());
        assertEquals(retryHandler.getRetryOptions().shouldRetry(), retryHandlerOption.shouldRetry());
    }

    @Test
    public void RetryHandlerWithRetryOptionConstructor() throws SecurityException, IllegalArgumentException {
        RetryHandlerOption retryHandlerOption = new RetryHandlerOption();
        RetryHandler retryHandler = new RetryHandler(retryHandlerOption);

        Request request = new Request.Builder().url("https://graph.microsoft.com/v1.0/me").build();
        Response response = new Response.Builder()
                .protocol(Protocol.HTTP_2)
                .code(HttpURLConnection.HTTP_GATEWAY_TIMEOUT)
                .message("Gateway Timeout")
                .request(request).build();

        assertTrue(retryHandler.retryRequest(response, 1, request, retryHandlerOption));
    }

    @Test
    public void RetryHandlerWithCustomOptions() throws SecurityException, IllegalAccessException {
        IShouldRetry shouldRetry = new IShouldRetry() {
            public boolean shouldRetry(long delay, int executionCount, Request request, Response response){
                return false;
            }
        };
        
        RetryHandlerOption retryHandlerOption = new RetryHandlerOption(shouldRetry, 5, 0);
        RetryHandler retryHandler = new RetryHandler(retryHandlerOption);

        Request request = new Request.Builder().url("https://graph.microsoft.com/v1.0/me").build();
        Response response = new Response.Builder()
                .protocol(Protocol.HTTP_2)
                .code(HttpURLConnection.HTTP_GATEWAY_TIMEOUT)
                .message("Gateway Timeout")
                .request(request).build();

        assertTrue(!retryHandler.retryRequest(response, 0, request, retryHandlerOption));
    }

    @Test
    public void TestRetryWithMaxRetryAttemps() {
        RetryHandler retryHandler = new RetryHandler();
        Request request = new Request.Builder().url("https://graph.microsoft.com/v1.0/me").build();

        Response response = new Response.Builder()
                .protocol(Protocol.HTTP_2)
                .code(HttpURLConnection.HTTP_GATEWAY_TIMEOUT)
                .message("Gateway Timeout")
                .request(request)
                .build();
        RetryHandlerOption retryHandlerOption = new RetryHandlerOption();
        int numberOfRetrys = RetryHandlerOption.DEFAULT_MAX_RETRIES + 1;

        assertFalse(retryHandler.retryRequest(response, numberOfRetrys, request, retryHandlerOption));
    }

    @Test
    public void TestRetryWithUnacceptableStatusCode() {
        RetryHandler retryHandler = new RetryHandler();

        Request request = new Request.Builder().url("https://graph.microsoft.com/v1.0/me").build();
        //Response with code 500 should not trigger a retry
        Response response = new Response.Builder()
                .protocol(Protocol.HTTP_2)
                .code(500)
                .message("Internal Server Error")
                .request(request)
                .build();

        assertFalse(retryHandler.retryRequest(response, 1, request, new RetryHandlerOption()));
    }

    @Test
    public void TestRetryWithTransferEncoding() {
        RetryHandler retryHandler = new RetryHandler();

        Request request = new Request.Builder().url("https://graph.microsoft.com/v1.0/me")
                .post(RequestBody.create("TEST", MediaType.parse("application/json"))).build();
        Response response = new Response.Builder()
                .protocol(Protocol.HTTP_2)
                .code(HttpURLConnection.HTTP_GATEWAY_TIMEOUT)
                .message("Gateway Timeout")
                .request(request)
                .addHeader("Transfer-Encoding", "chunked")
                .build();

        assertTrue(retryHandler.retryRequest(response, 1, request, new RetryHandlerOption()));
    }

    @Test
    public void TestRetryWithExponentialBackOff() {
        RetryHandler retryHandler = new RetryHandler();
        
        Request request = new Request.Builder().url("https://graph.microsoft.com/v1.0/me")
                .post(RequestBody.create("TEST", MediaType.parse("application/json"))).build();
        Response response = new Response.Builder()
                .protocol(Protocol.HTTP_2)
                .code(HttpURLConnection.HTTP_GATEWAY_TIMEOUT)
                .message("Gateway Timeout")
                .request(request)
                .addHeader("Transfer-Encoding", "chunked")
                .build();
                
        assertTrue(retryHandler.retryRequest(response, 1, request, new RetryHandlerOption()));
    }

    @Test
    public void TestGetRetryAfterUsingHeader() {
        RetryHandler retryHandler = new RetryHandler();
        long delay = retryHandler.getRetryAfter(tooManyRequestResponse().newBuilder().addHeader("Retry-After", "60").build(), 1, 1);
        assertTrue(delay == 60000);
        delay = retryHandler.getRetryAfter(tooManyRequestResponse().newBuilder().addHeader("Retry-After", "1").build(), 2, 3);
        assertTrue(delay == 1000);


        formatter.setTimeZone(TimeZone.getTimeZone(ZoneId.of("GMT")));
        Instant futureTime = Instant.now().plus(25, ChronoUnit.SECONDS); //Make the retry after time 25 seconds from time of running test
        String retryAfterString = formatter.format(new Date().from(futureTime));

        delay = retryHandler.getRetryAfter(tooManyRequestResponse().newBuilder().addHeader("Retry-After", retryAfterString).build(), 2, 3);
        assertTrue(delay > 23000); //Delay will not be exactly 25000 due to processing time, but should be close
        assertTrue(delay != 180000); //Ensure it has not simply fallen back to default

    }

    @Test 
    public void TestGetRetryOnFirstExecution() {
        RetryHandler retryHandler = new RetryHandler();
        long delay = retryHandler.getRetryAfter(tooManyRequestResponse(), 3, 1);
        assertTrue(delay>3000);
        delay = retryHandler.getRetryAfter(tooManyRequestResponse(), 3, 2);
        assertTrue(delay>4000);
    }

    @Test
    public void TestGetRetryAfterMaxDelayExceeded() {
        RetryHandler retryHandler = new RetryHandler();
        long delay = retryHandler.getRetryAfter(tooManyRequestResponse(), 190, 1);
        assertTrue(delay == 180000);

        //Ensure fallback to max works with retry after header of date type
        formatter.setTimeZone(TimeZone.getTimeZone(ZoneId.of("GMT")));
        Instant futureTime = Instant.now().plus(181, ChronoUnit.SECONDS);
        String retryAfterString = formatter.format(new Date().from(futureTime));

        delay = retryHandler.getRetryAfter(tooManyRequestResponse().newBuilder().addHeader("Retry-After", retryAfterString).build(), 2, 3);
        assertTrue(delay == 180000);

    }

    @Test
    public void TestNullLoggerHandling() throws ParseException {
        assertThrows(NullPointerException.class, () -> {
            new RetryHandler(null, new RetryHandlerOption());
        }, "logger cannot be null");
    }

    @Test
    public void testIsBuffered() {
        final RetryHandler retryHandler = new RetryHandler();
        Request request = new Request.Builder().url("https://localhost").method("GET", null).build();
        assertTrue(retryHandler.isBuffered(request), "Get Request is buffered");

        request = new Request.Builder().url("https://localhost").method("DELETE", null).build();
        assertTrue(retryHandler.isBuffered(request), "Delete Request is buffered");

        request = new Request.Builder().url("https://localhost")
                                        .method("POST",
                                            RequestBody.create("{\"key\": 42 }", MediaType.parse("application/json")))
                                        .build();
        assertTrue(retryHandler.isBuffered(request), "Post Request is buffered");

        request = new Request.Builder().url("https://localhost")
                                        .method("POST",
                                            new RequestBody() {

                                                @Override
                                                public MediaType contentType() {
                                                    return MediaType.parse("application/octet-stream");
                                                }

                                                @Override
                                                public void writeTo(BufferedSink sink) {
                                                    // TODO Auto-generated method stub

                                                }
                                            })
                                        .build();
        assertFalse(retryHandler.isBuffered(request), "Post Stream Request is not buffered");
    }
    
    Response tooManyRequestResponse() {
        return new Response.Builder()
                .code(429)
                .message("Too Many Requests")
                .request(new Request.Builder().url("https://graph.microsoft.com/v1.0/me").build())
                .protocol(Protocol.HTTP_2)
                .build();
    }

    

}
