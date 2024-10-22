import apisdk.ApiClient;
import apisdk.repos.item.item.generate.GeneratePostRequestBody;
import com.microsoft.kiota.authentication.AnonymousAuthenticationProvider;
import com.microsoft.kiota.http.OkHttpRequestAdapter;
import org.junit.jupiter.api.Test;

public class GHAPITest {

    @Test
    void basicTest() throws Exception {
        var adapter = new OkHttpRequestAdapter(new AnonymousAuthenticationProvider());
        adapter.setBaseUrl("http://127.0.0.1:1080");
        var client = new ApiClient(adapter);

        client.repos().byOrgId("my-owner").byRepoId("my-repo").get();
        final GeneratePostRequestBody body = new GeneratePostRequestBody();
        body.setDescription("my description");
        body.setIncludeAllBranches(true);
        body.setName("my-repo2");
        body.setOwner("my-owner2");
        body.setPrivate(false);
        client.repos().byOrgId("my-owner").byRepoId("my-repo").generate().post(body);
    }

}
