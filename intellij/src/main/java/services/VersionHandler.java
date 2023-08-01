package services;

import com.github.arteam.simplejsonrpc.client.builder.RequestBuilder;

import java.io.IOException;

public class VersionHandler {
    KiotaJavaClient client;
    public String getVersion() throws IOException {
        client = new KiotaJavaClient();
        RequestBuilder<String> requestBuilder = client.createRequest("GetVersion", String.class);
        String response = requestBuilder.execute();
        return response;
    }
}
