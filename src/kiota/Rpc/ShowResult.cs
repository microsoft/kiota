namespace kiota.Rpc;

public record ShowResult(
    List<LogEntry> logs, 
    PathItem? rootNode, 
    string? apiTitle, 
    IEnumerable<string>? servers = null, 
    IDictionary<string, SecurityRequirement>? securityRequirements = null, 
    IDictionary<string, SecuritySchemeInfo>? securitySchemes = null);

