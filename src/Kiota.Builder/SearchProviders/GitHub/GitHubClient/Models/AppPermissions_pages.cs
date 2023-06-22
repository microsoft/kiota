using System.Runtime.Serialization;
using System;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models {
    /// <summary>The level of permission to grant the access token to retrieve Pages statuses, configuration, and builds, as well as create new builds.</summary>
    public enum AppPermissions_pages {
        [EnumMember(Value = "read")]
        Read,
        [EnumMember(Value = "write")]
        Write,
    }
}
