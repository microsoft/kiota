using System.Collections.Generic;

namespace Kiota.Builder.SearchProviders.GitHub.Index;
public class BlockList {
    public List<string> Repositories { get; set; }
    public List<string> Organizations { get; set; }
}
