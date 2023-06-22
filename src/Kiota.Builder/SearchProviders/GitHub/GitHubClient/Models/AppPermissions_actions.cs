using System.Runtime.Serialization;
using System;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models {
    /// <summary>The level of permission to grant the access token for GitHub Actions workflows, workflow runs, and artifacts.</summary>
    public enum AppPermissions_actions {
        [EnumMember(Value = "read")]
        Read,
        [EnumMember(Value = "write")]
        Write,
    }
}
