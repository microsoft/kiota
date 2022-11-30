using System;

namespace Kiota.Builder.Configuration;

public class SearchConfiguration : SearchConfigurationBase {
    public Uri APIsGuruListUrl { get; set; } = new ("https://raw.githubusercontent.com/APIs-guru/openapi-directory/gh-pages/v2/list.json");
    public GitHubConfiguration GitHub { get; set; } = new();
}

public class GitHubConfiguration {
    public string AppId { get; set; } = "Iv1.9ed2bcb878c90617";
    public Uri ApiBaseUrl { get; set; } = new ("https://api.github.com");
    public Uri BlockListUrl { get; set; } = new ("https://raw.githubusercontent.com/microsoft/kiota/main/resources/index-block-list.yml");
    public Uri AppManagement { get; set; } = new("https://aka.ms/kiota/install/github");
}
