package com.microsoft.kiota.http.middleware.options;

import javax.annotation.Nonnull;

import okhttp3.Response;

/**
 * Indicates whether a specific response redirect information should be followed
 */
public interface IShouldRedirect {
    /**
     * Determines whether to follow the redirect information
     * @param response current response
     * @return whether the handler should follow the redirect information
     */
    boolean shouldRedirect(@Nonnull final Response response);
}
