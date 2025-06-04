using Kiota.Builder.OpenApiExtensions;
using Microsoft.OpenApi;

namespace kiota.Rpc
{
    internal class SecuritySchemeMapper
    {
        internal static Dictionary<string, SecuritySchemeInfo> FromComponents(OpenApiComponents? components)
        {
            var securitySchemes = new Dictionary<string, SecuritySchemeInfo>();
            var componentSchemes = components?.SecuritySchemes;
            if (componentSchemes is not null)
            {
                foreach (var (key, value) in componentSchemes)
                {
                    if (key != null && value != null)
                    {
                        var securitySchemeInfo = BuildSchemeInfoFromSecurityScheme(value);
                        securitySchemes[key] = securitySchemeInfo;
                    }
                }

            }
            return securitySchemes;
        }

        private static SecuritySchemeInfo BuildSchemeInfoFromSecurityScheme(IOpenApiSecurityScheme value)
        {
            string? description = value?.Description;
            string? @in = value?.In?.GetDisplayName();
            string? scheme = value?.Scheme;
            string? name = value?.Name;
            string? bearerFormat = value?.BearerFormat;
            string? openIdConnectUrl = value?.OpenIdConnectUrl?.OriginalString;
            var flows = BuildFlowsFromSecurityScheme(value);

            string? referenceId = null;
            if (value?.Extensions is not null)
            {
                var referenceExtensionFound = value.Extensions.TryGetValue(OpenApiAiAuthReferenceIdExtension.Name, out var authReferenceIdExtension);
                if (referenceExtensionFound)
                {
                    var typedReferenceExtension = authReferenceIdExtension as OpenApiAiAuthReferenceIdExtension;
                    referenceId = typedReferenceExtension?.AuthenticationReferenceId;
                }
            }
            var schemeType = value?.Type?.GetDisplayName() ?? "none";
            var securitySchemeInfo = new SecuritySchemeInfo(
                name: name,
                type: schemeType,
                description: description,
                @in: @in,
                scheme: scheme,
                bearerFormat: bearerFormat,
                openIdConnectUrl: openIdConnectUrl,
                flows: flows,
                referenceId: referenceId);
            return securitySchemeInfo;
        }

        private static OAuthFlows? BuildFlowsFromSecurityScheme(IOpenApiSecurityScheme? value)
        {
            return value?.Flows is null ? null : new OAuthFlows(
                @implicit: value.Flows.Implicit is null ? null : new OAuthFlow(
                    authorizationUrl: value.Flows.Implicit.AuthorizationUrl?.OriginalString,
                    tokenUrl: value.Flows.Implicit.TokenUrl?.OriginalString,
                    refreshUrl: value.Flows.Implicit.RefreshUrl?.OriginalString,
                    scopes: value.Flows.Implicit.Scopes),
                password: value.Flows.Password is null ? null : new OAuthFlow(
                    authorizationUrl: value.Flows.Password.AuthorizationUrl?.OriginalString,
                    tokenUrl: value.Flows.Password.TokenUrl?.OriginalString,
                    refreshUrl: value.Flows.Password.RefreshUrl?.OriginalString,
                    scopes: value.Flows.Password.Scopes),
                clientCredentials: value.Flows.ClientCredentials is null ? null : new OAuthFlow(
                    authorizationUrl: value.Flows.ClientCredentials.AuthorizationUrl?.OriginalString,
                    tokenUrl: value.Flows.ClientCredentials.TokenUrl?.OriginalString,
                    refreshUrl: value.Flows.ClientCredentials.RefreshUrl?.OriginalString,
                    scopes: value.Flows.ClientCredentials.Scopes),
                authorizationCode: value.Flows.AuthorizationCode is null ? null : new OAuthFlow(
                    authorizationUrl: value.Flows.AuthorizationCode.AuthorizationUrl?.OriginalString,
                    tokenUrl: value.Flows.AuthorizationCode.TokenUrl?.OriginalString,
                    refreshUrl: value.Flows.AuthorizationCode.RefreshUrl?.OriginalString,
                    scopes: value.Flows.AuthorizationCode.Scopes)
            );
        }
    }
}
