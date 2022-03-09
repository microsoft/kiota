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
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;

import javax.swing.text.html.FormSubmitEvent.MethodType;

import com.microsoft.kiota.HttpMethod;
import com.microsoft.kiota.RequestAdapter;
import com.microsoft.kiota.RequestInformation;
import com.microsoft.kiota.authentication.AuthenticationProvider;
import com.microsoft.kiota.http.middleware.TelemetryHandler;
import com.microsoft.kiota.http.middleware.options.TelemetryHandlerOption;

import okhttp3.*;
import org.junit.jupiter.api.Test;
import org.mockito.InjectMocks;

public class TelemetryHandlerTests {

    @InjectMocks
    public OkHttpRequestAdapter adapter = new OkHttpRequestAdapter(mock(AuthenticationProvider.class));

     @Test
     public void DefaultTelemetryHandlerDoesNotChangeRequest()  throws IOException, URISyntaxException, NoSuchMethodException, 
     InvocationTargetException, IllegalAccessException{
         Method method = OkHttpRequestAdapter.class.getDeclaredMethod("getRequestFromRequestInformation",RequestInformation.class);
         method.setAccessible(true);

         RequestInformation requestInfo = new RequestInformation();
         requestInfo.httpMethod = HttpMethod.GET;
         requestInfo.setUri(new URI("https://graph.microsoft.com/v1.0/users/"));

         TelemetryHandler telemetryHandler = new TelemetryHandler();
         Interceptor[] interceptorArr = {telemetryHandler};
         Request request = (Request) method.invoke(adapter, requestInfo);
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
        requestInfo.addRequestOptions(telemetryHandlerOption);

        Request request = (Request) method.invoke(adapter, requestInfo);
        OkHttpClient client = KiotaClientFactory.Create().build();
        Response response = client.newCall(request).execute();

        assertTrue(response.request().header("SdkVersion").contains("x.x.x"));
        assertEquals(1, response.request().headers().size());
    }

    @Test
    public void TelemetryHandlerGloballyEnrichesRequest()  throws IOException, URISyntaxException, NoSuchMethodException, 
    InvocationTargetException, IllegalAccessException{
        Method method = OkHttpRequestAdapter.class.getDeclaredMethod("getRequestFromRequestInformation",RequestInformation.class);
        method.setAccessible(true);

        RequestInformation requestInfo = new RequestInformation();
        requestInfo.httpMethod = HttpMethod.GET;
        requestInfo.setUri(new URI("https://graph.microsoft.com/v1.0/users/"));

        //Configure telemetry at the handler level
        TelemetryHandlerOption telemetryHandlerOption = new TelemetryHandlerOption();
        telemetryHandlerOption.TelemetryConfigurator = (request) -> {
          return request.newBuilder().addHeader("SdkVersion", "x.x.x").build();
        };

        TelemetryHandler telemetryHandler = new TelemetryHandler(telemetryHandlerOption);
        Interceptor[] interceptorArr = {telemetryHandler};
        Request request = (Request) method.invoke(adapter, requestInfo);
        OkHttpClient client = KiotaClientFactory.Create(interceptorArr).build();
        Response response = client.newCall(request).execute();

        assertTrue(response.request().header("SdkVersion").contains("x.x.x"));
        assertEquals(1, response.request().headers().size());
    }
}
