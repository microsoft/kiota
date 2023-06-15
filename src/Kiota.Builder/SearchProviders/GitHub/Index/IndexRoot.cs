using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Kiota.Builder.SearchProviders.GitHub.Index;


[JsonSerializable(typeof(IndexRoot))]
internal partial class IndexRootJsonContext : JsonSerializerContext
{
}
#pragma warning disable CA2227
#pragma warning disable CA1002
#pragma warning disable CA1056
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
#pragma warning restore CA1056
#pragma warning restore CA1002
#pragma warning restore CA2227
