namespace kiota.Rpc;

public record PathItem(
    string path,
    string segment,
    PathItem[] children,
    bool selected,
    bool isOperation = false,
    string? operationId = null,
    string? summary = null,
    string? description = null,
    Uri? documentationUrl = null,
    IEnumerable<string>? servers = null,
    IList<IDictionary<string, IList<string>?>>? security = null, // key is the security scheme name, value is the list of scopes
    AdaptiveCardInfo? adaptiveCard = null
);
