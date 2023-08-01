package services;
import com.github.arteam.simplejsonrpc.client.JsonRpcClient;
import com.github.arteam.simplejsonrpc.client.builder.RequestBuilder;
public class KiotaJavaClient{
    private final JsonRpcClient client;
    public KiotaJavaClient() {
        client = new JsonRpcClient(new ProcessTransport("kiota", "rpc"));
    }
    public <T> RequestBuilder<T> createRequest(String method, Class<T> returnType) {
        return client.createRequest()
                .method(method)
                .returnAs(returnType);
    }
}