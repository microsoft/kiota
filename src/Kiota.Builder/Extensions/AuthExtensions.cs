using Microsoft.DeclarativeAgents.Manifest;

namespace Kiota.Builder.Extensions
{
    public static class AuthExtensions
    {
        public static string? GetReferenceId(this Auth? auth)
        {
            if (auth == null)
            {
                return null;
            }
            else if (auth is OAuthPluginVault)
            {
                return (auth as OAuthPluginVault)?.ReferenceId;
            }
            else if (auth is ApiKeyPluginVault)
            {
                return (auth as ApiKeyPluginVault)?.ReferenceId;
            }
            else
            {
                return null;
            }
        }
    }
}
