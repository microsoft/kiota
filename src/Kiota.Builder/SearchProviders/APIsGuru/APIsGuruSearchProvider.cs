using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System;
using System.Text.Json;
using System.Linq;

namespace Kiota.Builder.SearchProviders.APIsGuru;

public class APIsGuruSearchProvider : ISearchProvider
{
    public Uri SearchUri { get; init; }
    public HttpClient HttpClient { get; init; }
    protected async Task<string> GetAPIsList(CancellationToken token) {
        return await HttpClient.GetStringAsync(SearchUri, token);
    }
    public async Task<IEnumerable<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        if (SearchUri == null)
            return Enumerable.Empty<SearchResult>();
        var rawDocument = await GetAPIsList(cancellationToken);
        var apiEntries = JsonSerializer.Deserialize<Dictionary<string, APIEntry>>(rawDocument);
        var results = apiEntries.Where(x => x.Key.Contains(query, StringComparison.OrdinalIgnoreCase))
                                .Select(static x => x.Value.versions.TryGetValue(x.Value.preferred, out var version) ? (x.Key, version) : (x.Key, default))
                                .Where(static x => x.version is not null)
                                .Select(x => new SearchResult(x.Key, x.version.info?.title, x.version.info?.description, x.version.info?.contact?.url, x.version.info?.origin?.FirstOrDefault()?.url));
        return results;
    }
}
