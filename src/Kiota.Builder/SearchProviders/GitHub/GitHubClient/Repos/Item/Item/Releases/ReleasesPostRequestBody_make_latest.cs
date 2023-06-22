using System.Runtime.Serialization;
using System;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Repos.Item.Item.Releases {
    /// <summary>Specifies whether this release should be set as the latest release for the repository. Drafts and prereleases cannot be set as latest. Defaults to `true` for newly published releases. `legacy` specifies that the latest release should be determined based on the release creation date and higher semantic version.</summary>
    public enum ReleasesPostRequestBody_make_latest {
        [EnumMember(Value = "true")]
        True,
        [EnumMember(Value = "false")]
        False,
        [EnumMember(Value = "legacy")]
        Legacy,
    }
}
