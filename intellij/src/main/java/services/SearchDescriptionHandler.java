package services;

import com.github.arteam.simplejsonrpc.client.builder.RequestBuilder;
public class SearchDescriptionHandler {
    KiotaJavaClient myClient;
    public KiotaSearchResult search(String searchTerm) {
        myClient = new KiotaJavaClient();
        RequestBuilder<KiotaSearchResult> requestBuilder = myClient.createRequest("Search", KiotaSearchResult.class);
        KiotaSearchResult response = requestBuilder.params(searchTerm).execute();

        if (response != null) {
            return response;
        } else {
            System.out.println("An error occurred");
            return null;
        }
    }
}