namespace kiota.Rpc;

public record PathItem(
    string path, 
    string segment, 
    PathItem[] children, 
    bool selected, 
    bool isOperation = false, 
    Uri? documentationUrl = null, 
    IEnumerable<string>? servers = null, 
    IDictionary<string, SecurityRequirement>? securityRequirements = null);

