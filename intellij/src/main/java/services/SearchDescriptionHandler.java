package services;

import com.github.arteam.simplejsonrpc.client.builder.RequestBuilder;

import java.util.Map;

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
  /**
   * for dubugging
    public static void main(String[] args) {
        KiotaSearchResult searchResult = search("github");

        if (searchResult != null) {
            Map<String, KiotaSearchResultItem> results = searchResult.getResults();

            // Process results as needed
            for (Map.Entry<String, KiotaSearchResultItem> entry : results.entrySet()) {
                KiotaSearchResultItem item = entry.getValue();
                System.out.println("Title: " + item.getTitle());
                System.out.println("Description: " + item.getDescription());
                System.out.println("Service URL: " + item.getServiceUrl());
                System.out.println("Description URL: " + item.getDescriptionUrl());
                System.out.println("Version Labels: " + item.getVersionLabels());
                System.out.println();  // Add a newline for separation}
            }
        }
    }
}
   **/