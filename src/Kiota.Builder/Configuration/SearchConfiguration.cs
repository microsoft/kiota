using System;
using System.Runtime.InteropServices;

namespace Kiota.Builder.Configuration;

public class SearchConfiguration : SearchConfigurationBase, ICloneable
{
    public Uri APIsGuruListUrl { get; set; } = new("https://raw.githubusercontent.com/APIs-guru/openapi-directory/gh-pages/v2/list.json");
    public GitHubConfiguration GitHub { get; set; } = new();
    public ApicurioConfiguration Apicurio { get; set; } = new();

    public object Clone()
    {
        return new SearchConfiguration
        {
            APIsGuruListUrl = new(APIsGuruListUrl.ToString(), UriKind.RelativeOrAbsolute),
            GitHub = (GitHubConfiguration)GitHub.Clone(),
            Apicurio = (ApicurioConfiguration)Apicurio.Clone(),
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

public class ApicurioConfiguration : ICloneable
{
    public enum ApicurioSearchBy
    {
        LABEL,
        PROPERTY
    }

    public Uri? ApiBaseUrl
    {
        get; set;
    }

    public Uri? UIBaseUrl
    {
        get; set;
    }

    public int ArtifactsLimit { get; set; } = 10;
    public int VersionsLimit { get; set; } = 100;

    public ApicurioSearchBy SearchBy { get; set; } = ApicurioSearchBy.LABEL;

    public object Clone()
    {
        return new ApicurioConfiguration
        {
            ApiBaseUrl = (ApiBaseUrl != null) ? new(ApiBaseUrl.ToString(), UriKind.RelativeOrAbsolute) : null,
            UIBaseUrl = (UIBaseUrl != null) ? new(UIBaseUrl.ToString(), UriKind.RelativeOrAbsolute) : null,
        };
    }
}
