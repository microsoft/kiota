using System.Collections.Generic;

namespace Kiota.Builder.SearchProviders.GitHub.Index;

public class IndexRoot
{
    public List<IndexApiEntry> Apis { get; set; } = new();
}
public class IndexApiEntry
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BaseURL { get; set; } = string.Empty;
    public List<IndexApiProperty> Properties { get; set; } = new();
}
public class IndexApiProperty
{
    public string Type { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
