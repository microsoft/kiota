using System.Runtime.Serialization;
using System;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models {
    /// <summary>The level of permission to grant the access token for viewing and managing fine-grained personal access token requests to an organization.</summary>
    public enum AppPermissions_organization_personal_access_tokens {
        [EnumMember(Value = "read")]
        Read,
        [EnumMember(Value = "write")]
        Write,
    }
}
