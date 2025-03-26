namespace kiota.Rpc;

public record SecuritySchemeInfo(
    string type,
    string name,
    string? description = null,
    string? @in = null,
    string? scheme = null,
    string? bearerFormat = null,
    string? openIdConnectUrl = null,
    OAuthFlows? flows = null,
    string? referenceId = null);

public record OAuthFlows(
    OAuthFlow? implicitFlow = null,
    OAuthFlow? passwordFlow = null,
    OAuthFlow? clientCredentialsFlow = null,
    OAuthFlow? authorizationCodeFlow = null);

public record OAuthFlow(
    string? authorizationUrl = null,
    string? tokenUrl = null,
    string? refreshUrl = null,
    IDictionary<string, string>? scopes = null);

