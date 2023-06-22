using System;
using System.Runtime.Serialization;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models
{
    /// <summary>The level of permission to grant the access token to manage the post-receive hooks for an organization.</summary>
    public enum AppPermissions_organization_hooks
    {
        [EnumMember(Value = "read")]
        Read,
        [EnumMember(Value = "write")]
        Write,
    }
}
