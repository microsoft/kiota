import apisdk.ApiClient;
import com.microsoft.kiota.authentication.AnonymousAuthenticationProvider;
import com.microsoft.kiota.http.OkHttpRequestAdapter;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;
import org.mockserver.client.MockServerClient;
import org.mockserver.model.MediaType;

import java.util.concurrent.ExecutionException;
import java.util.concurrent.TimeUnit;

import static org.mockserver.matchers.Times.exactly;
import static org.mockserver.model.HttpRequest.request;
import static org.mockserver.model.HttpResponse.response;

public class BasicAPITest {

    static String host = "127.0.0.1";
    static int port = 1080;

    @BeforeAll
    static void beforeAll() {
        new MockServerClient(host, port)
                .when(
                        request()
                                .withMethod("GET")
                                .withPath("/api/v1/topics"),
                        exactly(1)
                )
                .respond(
                        response()
                                .withContentType(MediaType.APPLICATION_JSON)
                                .withStatusCode(423)
                                .withBody("{ id: 'my-error', code: 123 }")
                );
    }

    @Test
    void basicTest() throws Exception {
        var adapter = new OkHttpRequestAdapter(new AnonymousAuthenticationProvider());
        adapter.setBaseUrl("http://" + host + ":" + port);
        var client = new ApiClient(adapter);

        var exception = Assertions.assertThrows(
                ExecutionException.class,
                () -> client.api().v1().topics().get().get(1, TimeUnit.SECONDS)
        );

        Assertions.assertNotNull(exception.getCause());
        Assertions.assertTrue(exception.getCause() instanceof apisdk.models.Error);
        var error = (apisdk.models.Error) exception.getCause();

        Assertions.assertEquals("my-error", error.getId());
        Assertions.assertEquals(123, error.getCode());
    }
}
