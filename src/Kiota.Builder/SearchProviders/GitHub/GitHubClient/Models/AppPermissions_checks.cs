using System;
using System.Runtime.Serialization;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models
{
    /// <summary>The level of permission to grant the access token for checks on code.</summary>
    public enum AppPermissions_checks
    {
        [EnumMember(Value = "read")]
        Read,
        [EnumMember(Value = "write")]
        Write,
    }
}
