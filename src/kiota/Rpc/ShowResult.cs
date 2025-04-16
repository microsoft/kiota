namespace kiota.Rpc;

public record ShowResult(
    List<LogEntry> logs,
    PathItem? rootNode,
    string? apiTitle,
    IEnumerable<string>? servers = null,
    IList<IDictionary<string, IList<string>?>>? security = null,
    IDictionary<string, SecuritySchemeInfo>? securitySchemes = null);

