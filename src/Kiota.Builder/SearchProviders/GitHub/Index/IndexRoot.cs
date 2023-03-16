using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Kiota.Builder.SearchProviders.GitHub.Index;


[JsonSerializable(typeof(IndexRoot))]
internal partial class IndexRootJsonContext : JsonSerializerContext
{
}
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
