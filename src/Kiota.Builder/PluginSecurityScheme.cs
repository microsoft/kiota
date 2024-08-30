namespace Kiota.Builder;
/// <summary>
/// Supported security schemes for generating plugin auth objects
/// </summary>
public enum PluginSecurityScheme
{
    ApiKey,
    Http,
    Oauth2,
    OpenIdConnect,
}
