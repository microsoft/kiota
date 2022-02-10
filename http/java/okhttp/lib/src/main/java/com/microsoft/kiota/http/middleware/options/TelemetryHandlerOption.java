package com.microsoft.kiota.http.middleware.options;

import java.util.function.Function;

import com.microsoft.kiota.RequestOption;

import okhttp3.Request;

/**
 * TelemetryHandlerOption class
 */
public class TelemetryHandlerOption implements RequestOption {

    /**
     * A delegate which can be called to configure the Request with desired telemetry values.
     */
    public Function<Request, Request> TelemetryConfigurator = (request) -> request;

}
