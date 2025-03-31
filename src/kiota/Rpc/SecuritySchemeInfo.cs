namespace kiota.Rpc;

public record SecuritySchemeInfo(
    string type,
    string? name,
    string? description = null,
    string? @in = null,
    string? scheme = null,
    string? bearerFormat = null,
    string? openIdConnectUrl = null,
    OAuthFlows? flows = null,
    string? referenceId = null);

public record OAuthFlows(
    OAuthFlow? @implicit = null,
    OAuthFlow? password = null,
    OAuthFlow? clientCredentials = null,
    OAuthFlow? authorizationCode = null);

public record OAuthFlow(
    string? authorizationUrl = null,
    string? tokenUrl = null,
    string? refreshUrl = null,
    IDictionary<string, string>? scopes = null);

