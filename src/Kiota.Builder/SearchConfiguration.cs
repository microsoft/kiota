using System;

namespace Kiota.Builder;

public class SearchConfiguration {
    public string SearchTerm { get; set; }
    public Uri APIsGuruListUrl { get; set; } = new ("https://raw.githubusercontent.com/APIs-guru/openapi-directory/gh-pages/v2/list.json");
    public bool ClearCache { get; set; }
    public string Version { get; set; }
}
