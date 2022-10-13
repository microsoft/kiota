using System;

namespace Kiota.Builder.Configuration;

public class SearchConfiguration : SearchConfigurationBase {
    public Uri APIsGuruListUrl { get; set; } = new ("https://raw.githubusercontent.com/APIs-guru/openapi-directory/gh-pages/v2/list.json");
}
