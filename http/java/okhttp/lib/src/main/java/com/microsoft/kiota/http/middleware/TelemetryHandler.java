package com.microsoft.kiota.http.middleware;

import java.io.IOException;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

import com.microsoft.kiota.http.middleware.options.TelemetryHandlerOption;

import okhttp3.Interceptor;
import okhttp3.Request;
import okhttp3.Response;

/**
 * TelemetryHandler implementation using OkHttp3
 */
public class TelemetryHandler implements Interceptor{

    private TelemetryHandlerOption _telemetryHandlerOption;

    /**
     * TelemetryHandler no param constructor
     */
    public TelemetryHandler() {
        this(null);
    }

    /**
     * TelemetryHandler constructor with passed in options.
     * @param telemetryHandlerOption The user specified telemetryHandlerOptions
     */
    public TelemetryHandler(@Nullable TelemetryHandlerOption telemetryHandlerOption) {
        if (telemetryHandlerOption == null) {
            this._telemetryHandlerOption = new TelemetryHandlerOption();
        }
        this._telemetryHandlerOption = telemetryHandlerOption;
    }

    /**
     * Send the HttpRequest after telemetryOptions are handled
     * @param chain
     * @return
     * @throws IOException
     */
    @Override
    public Response intercept(@Nonnull Chain chain) throws IOException {
        final Request request = chain.request();

        TelemetryHandlerOption telemetryHandlerOption = request.tag(TelemetryHandlerOption.class);
        if(telemetryHandlerOption == null) {
            telemetryHandlerOption = this._telemetryHandlerOption; 
        }
        
        //Use the TelemetryConfigurator set by the user to enrich the request as desired.
        if(telemetryHandlerOption.TelemetryConfigurator != null) {
            Request enrichedRequest = telemetryHandlerOption.TelemetryConfigurator.apply(request);
            return chain.proceed(enrichedRequest);
        }

        //Simply forward request if TelemetryConfigurator is set to null intentionally. 
        return chain.proceed(request);
    }
    
}
