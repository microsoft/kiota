package services;

import java.util.Map;

public class KiotaSearchResult extends KiotaLoggedResult {
    private Map<String, KiotaSearchResultItem> results;
    public Map<String, KiotaSearchResultItem> getResults() {
        return results;
    }
}