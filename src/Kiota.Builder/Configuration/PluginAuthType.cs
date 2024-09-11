namespace Kiota.Builder.Configuration;

/// <summary>
/// Supported plugin types
/// </summary>
public enum PluginAuthType
{
    /// <summary>
    /// OAuth authentication
    /// </summary>
    OAuthPluginVault,
    /// <summary>
    /// API key, HTTP Bearer token or OpenId Connect authentication
    /// </summary>
    ApiKeyPluginVault
}
