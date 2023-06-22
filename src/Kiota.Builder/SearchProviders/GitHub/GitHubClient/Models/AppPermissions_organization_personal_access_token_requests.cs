using System.Runtime.Serialization;
using System;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models {
    /// <summary>The level of permission to grant the access token for viewing and managing fine-grained personal access tokens that have been approved by an organization.</summary>
    public enum AppPermissions_organization_personal_access_token_requests {
        [EnumMember(Value = "read")]
        Read,
        [EnumMember(Value = "write")]
        Write,
    }
}
