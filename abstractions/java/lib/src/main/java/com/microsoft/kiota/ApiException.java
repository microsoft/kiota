package com.microsoft.kiota;

/** Parent type for exceptions thrown by the client when receiving failed responses to its requests. */
public class ApiException extends Exception {
    /** {@inheritdoc} */
    public ApiException() {
        super();
    }
    /** {@inheritdoc} */
    public ApiException(String message) {
        super(message);
    }
    /** {@inheritdoc} */
    public ApiException(String message, Throwable cause) {
        super(message, cause);
    }
    /** {@inheritdoc} */
    public ApiException(Throwable cause) {
        super(cause);
    }
}
