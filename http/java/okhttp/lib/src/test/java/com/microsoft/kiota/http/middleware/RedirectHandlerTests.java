package com.microsoft.kiota.http.middleware;

import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.junit.jupiter.api.Assertions.fail;

import java.io.IOException;
import java.net.HttpURLConnection;
import java.net.ProtocolException;

import com.microsoft.kiota.http.middleware.options.RedirectHandlerOption;

import okhttp3.MediaType;
import okhttp3.Protocol;
import okhttp3.Request;
import okhttp3.RequestBody;
import okhttp3.Response;
import okhttp3.internal.http.StatusLine;

import org.junit.jupiter.api.Test;

public class RedirectHandlerTests {

    String testurl = "https://graph.microsoft.com/v1.0";
    String differenthosturl = "https://graph.abc.com/v1.0/";
    String testmeurl = "https://graph.microsoft.com/v1.0/me/";

    @Test
    public void testIsRedirectedFailureByNoLocationHeader() throws IOException {
        RedirectHandler redirectHandler = new RedirectHandler();
        Request httpget = new Request.Builder().url(testmeurl).build();
        Response response = new Response.Builder()
                .protocol(Protocol.HTTP_2)
                .code(HttpURLConnection.HTTP_MOVED_TEMP)
                .message("Moved Temporarily")
                .request(httpget)
                .build();
        boolean isRedirected = redirectHandler.isRedirected(httpget, response, 0, new RedirectHandlerOption());
        assertTrue(!isRedirected);
    }

    @Test
    public void testIsRedirectedFailureByStatusCodeBadRequest() throws IOException {
        RedirectHandler redirectHandler = new RedirectHandler();
        Request httpget = new Request.Builder().url(testmeurl).build();
        Response response = new Response.Builder()
                .protocol(Protocol.HTTP_2)
                .code(HttpURLConnection.HTTP_BAD_REQUEST)
                .message( "Bad Request")
                .addHeader("location", testmeurl)
                .request(httpget)
                .build();
        boolean isRedirected = redirectHandler.isRedirected(httpget, response, 0, new RedirectHandlerOption());
        assertTrue(!isRedirected);
    }

    @Test
    public void testIsRedirectedSuccessWithStatusCodeMovedTemporarily() throws IOException {
        RedirectHandler redirectHandler = new RedirectHandler();
        Request httpget = new Request.Builder().url(testmeurl).build();
        Response response = new Response.Builder()
                .protocol(Protocol.HTTP_2)
                .code(HttpURLConnection.HTTP_MOVED_TEMP)
                .message("Moved Temporarily")
                .addHeader("location", testmeurl)
                .request(httpget)
                .build();
        boolean isRedirected = redirectHandler.isRedirected(httpget, response, 0, new RedirectHandlerOption());
        assertTrue(isRedirected);
    }

    @Test
    public void testIsRedirectedSuccessWithStatusCodeMovedPermanently() throws IOException {
        RedirectHandler redirectHandler = new RedirectHandler();
        Request httpget = new Request.Builder().url(testmeurl).build();
        Response response = new Response.Builder()
                .protocol(Protocol.HTTP_2)
                .code(HttpURLConnection.HTTP_MOVED_PERM)
                .message("Moved Permanently")
                .addHeader("location", testmeurl)
                .request(httpget)
                .build();
        boolean isRedirected = redirectHandler.isRedirected(httpget, response, 0, new RedirectHandlerOption());
        assertTrue(isRedirected);
    }

    @Test
    public void testIsRedirectedSuccessWithStatusCodeTemporaryRedirect() throws IOException {
        RedirectHandler redirectHandler = new RedirectHandler();
        Request httpget = new Request.Builder().url(testmeurl).build();
        Response response = new Response.Builder()
                .protocol(Protocol.HTTP_2)
                .code(StatusLine.HTTP_TEMP_REDIRECT)
                .message("Temporary Redirect")
                .addHeader("location", testmeurl)
                .request(httpget)
                .build();
        boolean isRedirected = redirectHandler.isRedirected(httpget, response,0,new RedirectHandlerOption());
        assertTrue(isRedirected);
    }

    @Test
    public void testIsRedirectedSuccessWithStatusCodeSeeOther() throws IOException {
        RedirectHandler redirectHandler = new RedirectHandler();
        Request httpget = new Request.Builder().url(testmeurl).build();
        Response response = new Response.Builder()
                .protocol(Protocol.HTTP_2)
                .code(HttpURLConnection.HTTP_SEE_OTHER)
                .message( "See Other")
                .addHeader("location", testmeurl)
                .request(httpget)
                .build();
        boolean isRedirected = redirectHandler.isRedirected(httpget, response,0,new RedirectHandlerOption());
        assertTrue(isRedirected);
    }

    @Test
    public void testGetRedirectForGetMethod() {
        RedirectHandler redirectHandler = new RedirectHandler();
        Request httpget = new Request.Builder().url(testurl).build();
        Response response = new Response.Builder()
                .protocol(Protocol.HTTP_2)
                .code(HttpURLConnection.HTTP_MOVED_TEMP)
                .message("Moved Temporarily")
                .addHeader("location", testmeurl)
                .request(httpget)
                .build();
        try {
            Request request = redirectHandler.getRedirect(httpget, response);
            assertTrue(request != null);
            final String method = request.method();
            assertTrue(method.equalsIgnoreCase("GET"));
        } catch (ProtocolException e) {
            e.printStackTrace();
            fail("Redirect handler testGetRedirectForGetMethod failure");
        }
    }

    @Test
    public void testGetRedirectForGetMethodForAuthHeader() {
        RedirectHandler redirectHandler = new RedirectHandler();
        Request httpget = new Request.Builder().url(testurl).header("Authorization", "TOKEN").build();
        Response response = new Response.Builder()
                .protocol(Protocol.HTTP_2)
                .code(HttpURLConnection.HTTP_MOVED_TEMP)
                .message("Moved Temporarily")
                .addHeader("location", differenthosturl)
                .request(httpget)
                .build();

        try {
            Request request = redirectHandler.getRedirect(httpget, response);
            assertTrue(request != null);
            final String method = request.method();
            assertTrue(method.equalsIgnoreCase("GET"));
            String header = request.header("Authorization");
            assertTrue(header == null);
        } catch (ProtocolException e) {
            e.printStackTrace();
            fail("Redirect handler testGetRedirectForGetMethodForAuthHeader failure");
        }
    }

    @Test
    public void testGetRedirectForHeadMethod() {
        RedirectHandler redirectHandler = new RedirectHandler();
        Request httphead = new Request.Builder().url(testurl).method("HEAD", null).build();
        Response response = new Response.Builder()
                .protocol(Protocol.HTTP_2)
                .code(HttpURLConnection.HTTP_MOVED_TEMP)
                .message("Moved Temporarily")
                .addHeader("location", testmeurl)
                .request(httphead)
                .build();
        try {
            Request request = redirectHandler.getRedirect(httphead, response);
            assertTrue(request != null);
            final String method = request.method();
            assertTrue(method.equalsIgnoreCase("HEAD"));
        } catch (ProtocolException e) {
            e.printStackTrace();
            fail("Redirect handler testGetRedirectForHeadMethod failure");
        }
    }

    @Test
    public void testGetRedirectForPostMethod() {
        RedirectHandler redirectHandler = new RedirectHandler();
        RequestBody body = RequestBody.create("", MediaType.parse("application/json"));
        Request httppost = new Request.Builder().url(testurl).post(body).build();
        Response response = new Response.Builder()
                .protocol(Protocol.HTTP_2)
                .code(HttpURLConnection.HTTP_MOVED_TEMP)
                .message("Moved Temporarily")
                .addHeader("location", testmeurl)
                .request(httppost)
                .build();
        try {
            Request request = redirectHandler.getRedirect(httppost, response);
            assertTrue(request != null);
            final String method = request.method();
            assertTrue(method.equalsIgnoreCase("POST"));
        } catch (ProtocolException e) {
            e.printStackTrace();
            fail("Redirect handler testGetRedirectForPostMethod failure");
        }
    }

    @Test
    public void testGetRedirectForPostMethodWithStatusCodeSeeOther() {
        RedirectHandler redirectHandler = new RedirectHandler();
        Request httppost = new Request.Builder().url(testurl).build();

        Response response = new Response.Builder()
                .protocol(Protocol.HTTP_2)
                .code(HttpURLConnection.HTTP_SEE_OTHER)
                .message("See Other")
                .addHeader("location", testmeurl)
                .request(httppost)
                .build();

        try {
            Request request = redirectHandler.getRedirect(httppost, response);
            assertTrue(request != null);
            final String method = request.method();
            assertTrue(method.equalsIgnoreCase("GET"));
        } catch (ProtocolException e) {
            e.printStackTrace();
            fail("Redirect handler testGetRedirectForPostMethod1 failure");
        }
    }

    @Test
    public void testGetRedirectForRelativeURL() throws ProtocolException {
        RedirectHandler redirectHandler = new RedirectHandler();
        Request httppost = new Request.Builder().url(testurl).build();

        Response response = new Response.Builder()
                .protocol(Protocol.HTTP_2)
                .code(HttpURLConnection.HTTP_SEE_OTHER)
                .message("See Other")
                .addHeader("location", "/testrelativeurl")
                .request(httppost)
                .build();

            Request request = redirectHandler.getRedirect(httppost, response);
            assertTrue(request.url().toString().compareTo(testurl+"/testrelativeurl") == 0);
    }

    @Test
    public void testGetRedirectRelativeLocationRequestURLwithSlash() throws ProtocolException {
        RedirectHandler redirectHandler = new RedirectHandler();
        Request httppost = new Request.Builder().url(testmeurl).build();

        Response response = new Response.Builder()
                .protocol(Protocol.HTTP_2)
                .code(HttpURLConnection.HTTP_SEE_OTHER)
                .message("See Other")
                .addHeader("location", "/testrelativeurl")
                .request(httppost)
                .build();
            Request request = redirectHandler.getRedirect(httppost, response);
            String expected = "https://graph.microsoft.com/v1.0/me/testrelativeurl";
            assertTrue(request.url().toString().compareTo(expected) == 0);
    }
    @Test
    public void testIsRedirectedIsFalseIfExceedsMaxRedirects() throws ProtocolException, IOException {
        RedirectHandlerOption options = new RedirectHandlerOption(0, null);
        RedirectHandler redirectHandler = new RedirectHandler(options);
        Request httppost = new Request.Builder().url(testmeurl).build();

        Response response = new Response.Builder()
                .protocol(Protocol.HTTP_2)
                .code(HttpURLConnection.HTTP_SEE_OTHER)
                .message("See Other")
                .addHeader("location", "/testrelativeurl")
                .request(httppost)
                .build();
        boolean isRedirected = redirectHandler.isRedirected(httppost, response, 1, options);
        assertFalse(isRedirected);
    }
}
