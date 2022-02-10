package com.microsoft.kiota.http.middleware.options;

import java.util.function.Function;

import com.microsoft.kiota.RequestOption;

import okhttp3.Request;

/**
 * Options to be passed to the telemetry middleware.
 */
public class TelemetryHandlerOption implements RequestOption {

    public Function<Request, Request> TelemetryConfigurator = (request) -> request;

}
