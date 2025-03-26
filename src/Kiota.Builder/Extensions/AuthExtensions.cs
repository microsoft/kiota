using Microsoft.DeclarativeAgents.Manifest;

namespace Kiota.Builder.Extensions
{
    public static class AuthExtensions
    {
        public static string? GetReferenceId(this Auth? auth)
        {
            if (auth is OAuthPluginVault oauth)
            {
                return oauth.ReferenceId;
            }
            else if (auth is ApiKeyPluginVault apiKey)
            {
                return apiKey.ReferenceId;
            }
            return null;
        }
    }
}
