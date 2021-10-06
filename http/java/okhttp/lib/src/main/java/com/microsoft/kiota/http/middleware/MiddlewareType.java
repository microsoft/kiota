package com.microsoft.kiota.http.middleware;

/**
 * The type of middleware to applied to the request/response
 */
public enum MiddlewareType {

    /**
     * Authentication Middleware
     */
    AUTHENTICATION,

    /**
     * Redirect Middleware
     */
    REDIRECT,

    /**
     * Retry Middleware
     */
    RETRY,

    /**
     * Not supported
     */
    NOT_SUPPORTED
}
