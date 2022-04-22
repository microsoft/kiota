package com.microsoft.kiota.http.middleware;

import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.Mockito.mock;

import java.io.IOException;
import java.net.URI;
import java.net.URISyntaxException;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.util.ArrayList;
import java.util.List;

import com.microsoft.kiota.RequestOption;
import com.microsoft.kiota.HttpMethod;
import com.microsoft.kiota.RequestInformation;
import com.microsoft.kiota.authentication.AuthenticationProvider;
import com.microsoft.kiota.http.KiotaClientFactory;
import com.microsoft.kiota.http.OkHttpRequestAdapter;
import com.microsoft.kiota.http.middleware.options.RetryHandlerOption;
import com.microsoft.kiota.http.middleware.options.TelemetryHandlerOption;

import okhttp3.*;
import org.junit.jupiter.api.Test;
import org.mockito.InjectMocks;

public class TelemetryHandlerTests {

    @InjectMocks
    public OkHttpRequestAdapter adapter = new OkHttpRequestAdapter(mock(AuthenticationProvider.class));

     @Test
     public void DefaultTelemetryHandlerDoesNotChangeRequest() throws IOException {

        TelemetryHandler telemetryHandler = new TelemetryHandler();
        Interceptor[] interceptorArr = {telemetryHandler};
        Request request = new Request.Builder().url("https://graph.microsoft.com/v1.0/users/").build();
        OkHttpClient.Builder builder = KiotaClientFactory.Create(interceptorArr);
        Response response = builder.build().newCall(request).execute();

        assertEquals(0, response.request().headers().size());
     }

    @Test
    public void TelemetryHandlerSelectivelyEnrichesRequestsBasedOnRequestMiddleWare() throws IOException, URISyntaxException, NoSuchMethodException, 
    InvocationTargetException, IllegalAccessException {
        Method method = OkHttpRequestAdapter.class.getDeclaredMethod("getRequestFromRequestInformation",RequestInformation.class);
        method.setAccessible(true);

        RequestInformation requestInfo = new RequestInformation();
        requestInfo.httpMethod = HttpMethod.GET;
        requestInfo.setUri(new URI("https://graph.microsoft.com/v1.0/users/"));

        //Configure telemetry at the request level
        TelemetryHandlerOption telemetryHandlerOption = new TelemetryHandlerOption();
        telemetryHandlerOption.TelemetryConfigurator = (request) -> {
            return request.newBuilder().addHeader("SdkVersion","x.x.x").build();
        };
        requestInfo.addRequestOptions(new ArrayList<RequestOption>() {{
            add(telemetryHandlerOption);
            add(new RetryHandlerOption());
        }});
        TelemetryHandler telemetryHandler = new TelemetryHandler();
        Interceptor[] interceptors = {telemetryHandler};

        Request request = (Request) method.invoke(adapter, requestInfo);
        OkHttpClient client = KiotaClientFactory.Create(interceptors).build();
        Response response = client.newCall(request).execute();

        assertTrue(response.request().header("SdkVersion").contains("x.x.x"));
        assertEquals(1, response.request().headers().size());
    }

    @Test
    public void TelemetryHandlerGloballyEnrichesRequest()  throws IOException {
        //Configure telemetry at the handler level
        TelemetryHandlerOption telemetryHandlerOption = new TelemetryHandlerOption();
        telemetryHandlerOption.TelemetryConfigurator = (request) -> {
          return request.newBuilder().addHeader("SdkVersion", "x.x.x").build();
        };

        TelemetryHandler telemetryHandler = new TelemetryHandler(telemetryHandlerOption);
        Interceptor[] interceptorArr = {telemetryHandler};
        Request request = new Request.Builder().url("https://graph.microsoft.com/v1.0/users/").build();
        OkHttpClient client = KiotaClientFactory.Create(interceptorArr).build();
        Response response = client.newCall(request).execute();

        assertTrue(response.request().header("SdkVersion").contains("x.x.x"));
        assertEquals(1, response.request().headers().size());
    }
}
