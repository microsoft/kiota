using System.Runtime.Serialization;
using System;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models {
    /// <summary>The level of permission to grant the access token to view and manage security events like code scanning alerts.</summary>
    public enum AppPermissions_security_events {
        [EnumMember(Value = "read")]
        Read,
        [EnumMember(Value = "write")]
        Write,
    }
}
