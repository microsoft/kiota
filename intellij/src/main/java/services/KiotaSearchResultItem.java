package services;

import com.fasterxml.jackson.annotation.JsonProperty;

import java.util.List;

public class KiotaSearchResultItem {
    @JsonProperty("Title")
    private String title;
    @JsonProperty("Description")
    private String description;
    @JsonProperty("ServiceUrl")
    private String serviceUrl;
    @JsonProperty("DescriptionUrl")
    private String descriptionUrl;
    @JsonProperty("VersionLabels")
    private List<String> versionLabels;

    public String getTitle() {
        return title;
    }

    public String getDescription() {
        return description;
    }

    public void setDescription(String description) {
        this.description = description;
    }

    public String getServiceUrl() {
        return serviceUrl;
    }

    public void setServiceUrl(String serviceUrl) {
        this.serviceUrl = serviceUrl;
    }

    public String getDescriptionUrl() {
        return descriptionUrl;
    }

    public void setDescriptionUrl(String descriptionUrl) {
        this.descriptionUrl = descriptionUrl;
    }
}