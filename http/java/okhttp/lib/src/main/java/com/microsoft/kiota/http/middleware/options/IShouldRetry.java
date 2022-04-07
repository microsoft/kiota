package com.microsoft.kiota.http.middleware.options;

import okhttp3.Request;
import okhttp3.Response;

import javax.annotation.Nonnull;

/**
 * Indicates whether a specific request should be retried
 */
public interface IShouldRetry {
    /**
     * Determines whether a specific request should be retried
     * @param delay the delay to wait before retrying
     * @param executionCount number of retry attempts
     * @param request current request
     * @param response current response
     * @return whether the specific request should be retried by the handler
     */
    boolean shouldRetry(long delay, int executionCount, @Nonnull final Request request, @Nonnull final Response response);
}
