using System;

namespace Kiota.Builder.Configuration;

public class SearchConfiguration : SearchConfigurationBase {
    public Uri APIsGuruListUrl { get; set; } = new ("https://raw.githubusercontent.com/APIs-guru/openapi-directory/gh-pages/v2/list.json");
    public Uri GitHubBlockListUrl { get; set; } = new ("https://raw.githubusercontent.com/microsoft/kiota/main/resources/index-block-list.yml");
    public string GitHubAppId { get; set; } = "0adc165b71f9824f4282";
}
