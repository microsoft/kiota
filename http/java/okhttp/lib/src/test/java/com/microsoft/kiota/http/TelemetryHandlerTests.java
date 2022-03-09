package com.microsoft.kiota.http;
import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.Mockito.mock;

import java.io.IOException;
import java.net.HttpURLConnection;
import java.net.MalformedURLException;
import java.net.URI;
import java.net.URISyntaxException;
import java.util.ArrayList;
import java.util.Collection;
import java.util.List;
import java.util.concurrent.CompletableFuture;

import javax.swing.text.html.FormSubmitEvent.MethodType;

import com.microsoft.kiota.HttpMethod;
import com.microsoft.kiota.RequestInformation;
import com.microsoft.kiota.RequestOption;
import com.microsoft.kiota.ResponseHandler;
import com.microsoft.kiota.authentication.AuthenticationProvider;
import com.microsoft.kiota.http.OkHttpRequestAdapter;
import com.microsoft.kiota.http.middleware.TelemetryHandler;
import com.microsoft.kiota.http.middleware.options.RetryHandlerOption;
import com.microsoft.kiota.http.middleware.options.TelemetryHandlerOption;

import okhttp3.*;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.Assertions.*;

import okhttp3.Interceptor.Chain;
import okhttp3.internal.connection.RealCall.AsyncCall;

public class TelemetryHandlerTests {
    
    private OkHttpRequestAdapter requestAdapter;
    //private MockParsableClass mockTargetClass;// = new MockParsableClass();

    public void telemetryInitTests() {
        final TelemetryHandler telemetryHandler = new TelemetryHandler();
        assertNotNull(telemetryHandler);
        requestAdapter = new OkHttpRequestAdapter(mock(AuthenticationProvider.class));
    }

     @Test
     public void DefaultTelemetryHandlerDoesNotChangeRequest() throws URISyntaxException, IOException {
        
         //final TelemetryHandler telemetryHandler = new TelemetryHandler();
         requestAdapter = new OkHttpRequestAdapter(mock(AuthenticationProvider.class));

         RequestInformation requestInfo = new RequestInformation();
         requestInfo.httpMethod = HttpMethod.GET;
         requestInfo.setUri(new URI("https://graph.microsoft.com/v1.0/users/"));

         Request request = requestAdapter.getRequestFromRequestInformation(requestInfo);
         OkHttpClient.Builder builder = KiotaClientFactory.Create();
         Response response = builder.build().newCall(request).execute();

         assertEquals(0, response.request().headers().size());
     }

    @Test
    public void TelemetryHandlerSelectivelyEnrichesRequestsBasedOnRequestMiddleWare() throws IOException, URISyntaxException {
        requestAdapter = new OkHttpRequestAdapter(mock(AuthenticationProvider.class));

        RequestInformation requestInfo = new RequestInformation();
        requestInfo.httpMethod = HttpMethod.GET;
        requestInfo.setUri(new URI("https://graph.microsoft.com/v1.0/users/"));

        TelemetryHandlerOption telemetryHandlerOption = new TelemetryHandlerOption();
        telemetryHandlerOption.TelemetryConfigurator = (request) -> {
            return request.newBuilder().addHeader("SdkVersion","x.x.x").build();
        };

        requestInfo.addRequestOptions(telemetryHandlerOption);

        Request request = requestAdapter.getRequestFromRequestInformation(requestInfo);
        OkHttpClient client = KiotaClientFactory.Create().build();
        Response response = client.newCall(request).execute();

        assertTrue(response.request().header("SdkVersion").contains("x.x.x"));

    }


}
