using System.Collections.Generic;

namespace Kiota.Builder.SearchProviders.GitHub.Index;

public class IndexRoot {
    public List<IndexApiEntry> Apis { get; set; }
}
public class IndexApiEntry {
    public string Name { get; set; }
    public string Description { get; set; }
    public string BaseURL { get; set; }
    public List<IndexApiProperty> Properties { get; set; }
}
public class IndexApiProperty {
    public string Type { get; set; }
    public string Url { get; set; }
}
