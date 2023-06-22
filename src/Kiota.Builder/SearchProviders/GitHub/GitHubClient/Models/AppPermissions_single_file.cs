using System;
using System.Runtime.Serialization;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models
{
    /// <summary>The level of permission to grant the access token to manage just a single file.</summary>
    public enum AppPermissions_single_file
    {
        [EnumMember(Value = "read")]
        Read,
        [EnumMember(Value = "write")]
        Write,
    }
}
