import apisdk.ApiClient;
import com.microsoft.kiota.ApiException;
import com.microsoft.kiota.authentication.AnonymousAuthenticationProvider;
import com.microsoft.kiota.http.OkHttpRequestAdapter;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;

import java.util.concurrent.TimeUnit;

import static org.junit.jupiter.api.Assertions.assertEquals;

public class BasicAPITest {

    @Test
    void includesSomeQueryParameter() throws Exception {
        var client = new ApiClient(new OkHttpRequestAdapter(new AnonymousAuthenticationProvider()));

        var reqInf = client.api().something().v1().toGetRequestInformation(config -> {
            config.queryParameters.startDateTime = "START";
        });
        reqInf.pathParameters.put("baseurl", "http://test");

        assertEquals("http://test/api/something/v1?startDateTime=START", reqInf.getUri().toString());
    }

    @Test
    void includesSomeOtherQueryParameter() throws Exception {
        var client = new ApiClient(new OkHttpRequestAdapter(new AnonymousAuthenticationProvider()));

        var reqInf = client.api().something().v1().toGetRequestInformation(config -> {
            config.queryParameters.endDateTime = "END";
        });
        reqInf.pathParameters.put("baseurl", "http://test");

        assertEquals("http://test/api/something/v1?EndDateTime=END", reqInf.getUri().toString());
    }

    @Test
    void includesAllTheQueryParameters() throws Exception {
        var client = new ApiClient(new OkHttpRequestAdapter(new AnonymousAuthenticationProvider()));

        var reqInf = client.api().something().v1().toGetRequestInformation(config -> {
            config.queryParameters.startDateTime = "START";
            config.queryParameters.endDateTime = "END";
        });
        reqInf.pathParameters.put("baseurl", "http://test");

        assertEquals("http://test/api/something/v1?EndDateTime=END&startDateTime=START", reqInf.getUri().toString());
    }

}
