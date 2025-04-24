namespace kiota.Rpc;

public enum OpenApiTreeSpecVersion
{
    OpenApi2_0 = 0,
    OpenApi3_0 = 1,
    OpenApi3_1 = 2,
}

public record ShowResult(
    OpenApiTreeSpecVersion? specVersion,
    List<LogEntry> logs,
    PathItem? rootNode,
    string? apiTitle,
    IEnumerable<string>? servers = null,
    IList<IDictionary<string, IList<string>?>>? security = null,
    IDictionary<string, SecuritySchemeInfo>? securitySchemes = null);

