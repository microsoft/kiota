import apisdk.ApiClient;
import com.microsoft.kiota.ApiException;
import com.microsoft.kiota.authentication.AnonymousAuthenticationProvider;
import com.microsoft.kiota.http.OkHttpRequestAdapter;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;

import java.util.concurrent.TimeUnit;

public class BasicAPITest {

    @Test
    void basicTest() throws Exception {
        var adapter = new OkHttpRequestAdapter(new AnonymousAuthenticationProvider());
        adapter.setBaseUrl("http://127.0.0.1:1080");
        var client = new ApiClient(adapter);

        client.repos().byOwnerId("my-owner").byReposId("my-repo").get();
        client.repos().byTemplateOwner("my-template-owner").byTemplateRepo("my-repo").post(null);
    }

}
