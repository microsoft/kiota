using Kiota.Builder.OpenApiExtensions;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Models.Interfaces;

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
                        var securitySchemeInfo = BuildSchemeInfoFromSecurityScheme(key, value);
                        securitySchemes[key] = securitySchemeInfo;
                    }
                }

            }
            return securitySchemes;
        }

        private static SecuritySchemeInfo BuildSchemeInfoFromSecurityScheme(string schemeName, IOpenApiSecurityScheme value)
        {
            string? description = value?.Description;
            string? @in = value?.In?.GetDisplayName() ?? "testvalue";
            string? scheme = value?.Scheme;
            string? bearerFormat = value?.BearerFormat;
            string? openIdConnectUrl = value?.OpenIdConnectUrl?.AbsoluteUri;
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
                name: schemeName,
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
                implicitFlow: value.Flows.Implicit is null ? null : new OAuthFlow(
                    authorizationUrl: value.Flows.Implicit.AuthorizationUrl?.AbsoluteUri,
                    tokenUrl: value.Flows.Implicit.TokenUrl?.AbsoluteUri,
                    refreshUrl: value.Flows.Implicit.RefreshUrl?.AbsoluteUri,
                    scopes: value.Flows.Implicit.Scopes),
                passwordFlow: value.Flows.Password is null ? null : new OAuthFlow(
                    authorizationUrl: value.Flows.Password.AuthorizationUrl?.AbsoluteUri,
                    tokenUrl: value.Flows.Password.TokenUrl?.AbsoluteUri,
                    refreshUrl: value.Flows.Password.RefreshUrl?.AbsoluteUri,
                    scopes: value.Flows.Password.Scopes),
                clientCredentialsFlow: value.Flows.ClientCredentials is null ? null : new OAuthFlow(
                    authorizationUrl: value.Flows.ClientCredentials.AuthorizationUrl?.AbsoluteUri,
                    tokenUrl: value.Flows.ClientCredentials.TokenUrl?.AbsoluteUri,
                    refreshUrl: value.Flows.ClientCredentials.RefreshUrl?.AbsoluteUri,
                    scopes: value.Flows.ClientCredentials.Scopes),
                authorizationCodeFlow: value.Flows.AuthorizationCode is null ? null : new OAuthFlow(
                    authorizationUrl: value.Flows.AuthorizationCode.AuthorizationUrl?.AbsoluteUri,
                    tokenUrl: value.Flows.AuthorizationCode.TokenUrl?.AbsoluteUri,
                    refreshUrl: value.Flows.AuthorizationCode.RefreshUrl?.AbsoluteUri,
                    scopes: value.Flows.AuthorizationCode.Scopes)
            );
        }
    }
}
