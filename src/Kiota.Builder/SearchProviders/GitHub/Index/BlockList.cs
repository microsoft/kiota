using System.Collections.Generic;

namespace Kiota.Builder.SearchProviders.GitHub.Index;
#pragma warning disable CA2227
#pragma warning disable CA1002
public class BlockList
{
    public List<string> Repositories { get; set; } = new();
    public List<string> Organizations { get; set; } = new();
}
#pragma warning restore CA1002
#pragma warning restore CA2227
