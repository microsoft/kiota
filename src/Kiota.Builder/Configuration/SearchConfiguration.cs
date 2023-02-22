using System;

namespace Kiota.Builder.Configuration;

public class SearchConfiguration : SearchConfigurationBase, ICloneable
{
    public Uri APIsGuruListUrl { get; set; } = new("https://raw.githubusercontent.com/APIs-guru/openapi-directory/gh-pages/v2/list.json");
    public GitHubConfiguration GitHub { get; set; } = new();

    public object Clone()
    {
        return new SearchConfiguration
        {
            APIsGuruListUrl = new(APIsGuruListUrl.ToString(), UriKind.RelativeOrAbsolute),
            GitHub = (GitHubConfiguration)GitHub.Clone(),
            ClearCache = ClearCache
        };
    }
}

public class GitHubConfiguration : ICloneable
{
    public string AppId { get; set; } = "Iv1.9ed2bcb878c90617";
    public Uri ApiBaseUrl { get; set; } = new("https://api.github.com");
    public Uri BlockListUrl { get; set; } = new("https://raw.githubusercontent.com/microsoft/kiota/main/resources/index-block-list.yml");
    public Uri AppManagement { get; set; } = new("https://aka.ms/kiota/install/github");

    public object Clone()
    {
        return new GitHubConfiguration
        {
            AppId = AppId,
            ApiBaseUrl = new(ApiBaseUrl.ToString(), UriKind.RelativeOrAbsolute),
            BlockListUrl = new(BlockListUrl.ToString(), UriKind.RelativeOrAbsolute),
            AppManagement = new(AppManagement.ToString(), UriKind.RelativeOrAbsolute)
        };
    }
}
