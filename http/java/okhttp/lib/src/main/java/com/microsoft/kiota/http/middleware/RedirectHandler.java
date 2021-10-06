package com.microsoft.kiota.http.middleware;

import static java.net.HttpURLConnection.HTTP_MOVED_PERM;
import static java.net.HttpURLConnection.HTTP_MOVED_TEMP;
import static java.net.HttpURLConnection.HTTP_SEE_OTHER;
import static okhttp3.internal.http.StatusLine.HTTP_PERM_REDIRECT;
import static okhttp3.internal.http.StatusLine.HTTP_TEMP_REDIRECT;

import java.io.IOException;
import java.net.ProtocolException;

import javax.annotation.Nullable;
import javax.annotation.Nonnull;

import com.microsoft.kiota.http.middleware.MiddlewareType;
import com.microsoft.kiota.http.middleware.options.RedirectOptions;
import com.microsoft.kiota.http.middleware.options.TelemetryOptions;

import okhttp3.HttpUrl;
import okhttp3.Interceptor;
import okhttp3.Request;
import okhttp3.Response;

/**
 * Middleware that determines whether a redirect information should be followed or not, and follows it if necessary.
 */
public class RedirectHandler implements Interceptor{

    /**
     * The current middleware type
     */
    public final MiddlewareType MIDDLEWARE_TYPE = MiddlewareType.REDIRECT;

    private RedirectOptions mRedirectOptions;

    /**
     * Initialize using default redirect options, default IShouldRedirect and max redirect value
     */
    public RedirectHandler() {
        this(null);
    }

    /**
     * Initialize using custom redirect options.
     * @param redirectOptions pass instance of redirect options to be used
     */
    public RedirectHandler(@Nullable final RedirectOptions redirectOptions) {
        this.mRedirectOptions = redirectOptions;
        if(redirectOptions == null) {
            this.mRedirectOptions = new RedirectOptions();
        }
    }

    boolean isRedirected(Request request, Response response, int redirectCount, RedirectOptions redirectOptions) throws IOException {
        // Check max count of redirects reached
        if(redirectCount > redirectOptions.maxRedirects()) return false;

        // Location header empty then don't redirect
        final String locationHeader = response.header("location");
        if(locationHeader == null)
            return false;

        // If any of 301,302,303,307,308 then redirect
        final int statusCode = response.code();
        if(statusCode == HTTP_PERM_REDIRECT || //308
                statusCode == HTTP_MOVED_PERM || //301
                statusCode == HTTP_TEMP_REDIRECT || //307
                statusCode == HTTP_SEE_OTHER || //303
                statusCode == HTTP_MOVED_TEMP) //302
            return true;

        return false;
    }

    Request getRedirect(
            final Request request,
            final Response userResponse) throws ProtocolException {
        String location = userResponse.header("Location");
        if (location == null || location.length() == 0) return null;

        // For relative URL in location header, the new url to redirect is relative to original request
        if(location.startsWith("/")) {
            if(request.url().toString().endsWith("/")) {
                location = location.substring(1);
            }
            location = request.url() + location;
        }

        HttpUrl requestUrl = userResponse.request().url();

        HttpUrl locationUrl = userResponse.request().url().resolve(location);

        // Don't follow redirects to unsupported protocols.
        if (locationUrl == null) return null;

        // Most redirects don't include a request body.
        Request.Builder requestBuilder = userResponse.request().newBuilder();

        // When redirecting across hosts, drop all authentication headers. This
        // is potentially annoying to the application layer since they have no
        // way to retain them.
        boolean sameScheme = locationUrl.scheme().equalsIgnoreCase(requestUrl.scheme());
        boolean sameHost = locationUrl.host().toString().equalsIgnoreCase(requestUrl.host().toString());
        if (!sameScheme || !sameHost) {
            requestBuilder.removeHeader("Authorization");
        }

        // Response status code 303 See Other then POST changes to GET
        if(userResponse.code() == HTTP_SEE_OTHER) {
            requestBuilder.method("GET", null);
        }

        return requestBuilder.url(locationUrl).build();
    }

    // Intercept request and response made to network
    @Override
    @Nonnull
    public Response intercept(@Nonnull final Chain chain) throws IOException {
        Request request = chain.request();

        TelemetryOptions telemetryOptions = request.tag(TelemetryOptions.class);
        if(telemetryOptions == null) {
            telemetryOptions = new TelemetryOptions();
            request = request.newBuilder().tag(TelemetryOptions.class, telemetryOptions).build();
        }
        telemetryOptions.setFeatureUsage(TelemetryOptions.REDIRECT_HANDLER_ENABLED_FLAG);

        Response response = null;
        int requestsCount = 1;

        // Use should retry pass along with this request
        RedirectOptions redirectOptions = request.tag(RedirectOptions.class);
        redirectOptions = redirectOptions != null ? redirectOptions : this.mRedirectOptions;

        while(true) {
            response = chain.proceed(request);
            final boolean shouldRedirect = isRedirected(request, response, requestsCount, redirectOptions)
                    && redirectOptions.shouldRedirect().shouldRedirect(response);
            if(!shouldRedirect) break;

            final Request followup = getRedirect(request, response);
            if(followup != null) {
                response.close();
                request = followup;
                requestsCount++;
            }
        }
        return response;
    }
}
