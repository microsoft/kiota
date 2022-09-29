using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kiota.Builder.SearchProviders;

public interface ISearchProvider {
    Task<IEnumerable<SearchResult>> SearchAsync(string term, CancellationToken cancellationToken);
}
