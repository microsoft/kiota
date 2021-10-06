package com.microsoft.kiota.http.middleware;

import java.io.IOException;
import java.util.Objects;

import javax.annotation.Nullable;
import javax.annotation.Nonnull;

import com.microsoft.kiota.http.middleware.IShouldRetry;
import com.microsoft.kiota.http.middleware.MiddlewareType;
import com.microsoft.kiota.http.middleware.options.RetryOptions;
import com.microsoft.kiota.http.middleware.options.TelemetryOptions;
import com.microsoft.kiota.http.logger.DefaultLogger;
import com.microsoft.kiota.http.logger.ILogger;

import okhttp3.Interceptor;
import okhttp3.Request;
import okhttp3.RequestBody;
import okhttp3.Response;
import okhttp3.ResponseBody;

/**
 * The middleware responsible for retrying requests when they fail because of transient issues
 */
public class RetryHandler implements Interceptor{

    /**
     * Type of the current middleware
     */
    public final MiddlewareType MIDDLEWARE_TYPE = MiddlewareType.RETRY;

    private RetryOptions mRetryOption;

    /**
     * Header name to track the retry attempt number
     */
    private static final String RETRY_ATTEMPT_HEADER = "Retry-Attempt";
    /**
     * Header name for the retry after information
     */
    private static final String RETRY_AFTER = "Retry-After";

    /**
     * Too many requests status code
     */
    public static final int MSClientErrorCodeTooManyRequests = 429;
    /**
     * Service unavailable status code
     */
    public static final int MSClientErrorCodeServiceUnavailable  = 503;
    /**
     * Gateway timeout status code
     */
    public static final int MSClientErrorCodeGatewayTimeout = 504;

    /**
     * One second as milliseconds
     */
    private static final long DELAY_MILLISECONDS = 1000;

    private final ILogger logger;

    /**
     * @param retryOption Create Retry handler using retry option
     */
    public RetryHandler(@Nullable final RetryOptions retryOption) {
        this(new DefaultLogger(), retryOption);
    }
    /**
     * @param retryOption Create Retry handler using retry option
     * @param logger logger to use for telemetry
     */
    public RetryHandler(@Nonnull final ILogger logger, @Nullable final RetryOptions retryOption) {
        this.logger = Objects.requireNonNull(logger, "logger parameter cannot be null");
        this.mRetryOption = retryOption;
        if(this.mRetryOption == null) {
            this.mRetryOption = new RetryOptions();
        }
    }
    /**
     * Initialize retry handler with default retry option
     */
    public RetryHandler() {
        this(null);
    }

    boolean retryRequest(Response response, int executionCount, Request request, RetryOptions retryOptions) {

        // Should retry option
        // Use should retry common for all requests
        IShouldRetry shouldRetryCallback = null;
        if(retryOptions != null) {
            shouldRetryCallback = retryOptions.shouldRetry();
        }

        boolean shouldRetry = false;
        // Status codes 429 503 504
        int statusCode = response.code();
        // Only requests with payloads that are buffered/rewindable are supported.
        // Payloads with forward only streams will be have the responses returned
        // without any retry attempt.
        shouldRetry =
                retryOptions != null
                        && executionCount <= retryOptions.maxRetries()
                        && checkStatus(statusCode) && isBuffered(request)
                        && shouldRetryCallback != null
                        && shouldRetryCallback.shouldRetry(retryOptions.delay(), executionCount, request, response);

        if(shouldRetry) {
            long retryInterval = getRetryAfter(response, retryOptions.delay(), executionCount);
            try {
                Thread.sleep(retryInterval);
            } catch (InterruptedException e) {
                Thread.currentThread().interrupt();
                logger.logError("error retrying the request", e);
            }
        }
        return shouldRetry;
    }

    /**
     * Get retry after in milliseconds
     * @param response Response
     * @param delay Delay in seconds
     * @param executionCount Execution count of retries
     * @return Retry interval in milliseconds
     */
    long getRetryAfter(Response response, long delay, int executionCount) {
        String retryAfterHeader = response.header(RETRY_AFTER);
        double retryDelay = RetryOptions.DEFAULT_DELAY * DELAY_MILLISECONDS;
        if(retryAfterHeader != null) {
            retryDelay = Long.parseLong(retryAfterHeader) * DELAY_MILLISECONDS;
        } else {
            retryDelay = (double)((Math.pow(2.0, (double)executionCount)-1)*0.5);
            retryDelay = (executionCount < 2 ? delay : retryDelay + delay) + (double)Math.random();
            retryDelay *= DELAY_MILLISECONDS;
        }
        return (long)Math.min(retryDelay, RetryOptions.MAX_DELAY * DELAY_MILLISECONDS);
    }

    boolean checkStatus(int statusCode) {
        return (statusCode == MSClientErrorCodeTooManyRequests || statusCode == MSClientErrorCodeServiceUnavailable
                || statusCode == MSClientErrorCodeGatewayTimeout);
    }

    boolean isBuffered(final Request request) {
        final String methodName = request.method();

        final boolean isHTTPMethodPutPatchOrPost = methodName.equalsIgnoreCase("POST") ||
                methodName.equalsIgnoreCase("PUT") ||
                methodName.equalsIgnoreCase("PATCH");

        final RequestBody requestBody = request.body();
        if(isHTTPMethodPutPatchOrPost && requestBody != null) {
            try {
                return requestBody.contentLength() != -1L;
            } catch (IOException ex) {
                // expected
                return false;
            }
        }
        return true;
    }

    @Override
    @Nonnull
    public Response intercept(@Nonnull final Chain chain) throws IOException {
        Request request = chain.request();

        TelemetryOptions telemetryOptions = request.tag(TelemetryOptions.class);
        if(telemetryOptions == null) {
            telemetryOptions = new TelemetryOptions();
            request = request.newBuilder().tag(TelemetryOptions.class, telemetryOptions).build();
        }
        telemetryOptions.setFeatureUsage(TelemetryOptions.RETRY_HANDLER_ENABLED_FLAG);

        Response response = chain.proceed(request);

        // Use should retry pass along with this request
        RetryOptions retryOption = request.tag(RetryOptions.class);
        retryOption = retryOption != null ? retryOption : mRetryOption;

        int executionCount = 1;
        while(retryRequest(response, executionCount, request, retryOption)) {
            request = request.newBuilder().addHeader(RETRY_ATTEMPT_HEADER, String.valueOf(executionCount)).build();
            executionCount++;
            if(response != null) {
                final ResponseBody body = response.body();
                if(body != null)
                    body.close();
                response.close();
            }
            response = chain.proceed(request);
        }
        return response;
    }

}
