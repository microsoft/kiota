import apisdk.ApiClient;
import com.microsoft.kiota.authentication.AnonymousAuthenticationProvider;
import com.microsoft.kiota.http.OkHttpRequestAdapter;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;

import java.util.concurrent.ExecutionException;
import java.util.concurrent.TimeUnit;

public class BasicAPITest {

    @Test
    void basicTest() throws Exception {
        var adapter = new OkHttpRequestAdapter(new AnonymousAuthenticationProvider());
        adapter.setBaseUrl("http://127.0.0.1:1080");
        var client = new ApiClient(adapter);

        var exception = Assertions.assertThrows(
                ExecutionException.class,
                () -> client.api().v1().topics().get().get(1, TimeUnit.SECONDS)
        );

        Assertions.assertNotNull(exception.getCause());
        Assertions.assertTrue(exception.getCause() instanceof com.microsoft.kiota.ApiException);
        var error = (com.microsoft.kiota.ApiException) exception.getCause();

        Assertions.assertEquals(423, error.getResponseStatusCode());
    }

}
