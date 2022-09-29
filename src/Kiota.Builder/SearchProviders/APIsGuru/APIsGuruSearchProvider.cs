using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System;
using System.Text.Json;
using System.Linq;
using Microsoft.Extensions.Logging;
using Kiota.Builder.Caching;
using System.IO;

namespace Kiota.Builder.SearchProviders.APIsGuru;

public class APIsGuruSearchProvider : ISearchProvider
{
    private readonly Uri SearchUri;
    private readonly DocumentCachingProvider cachingProvider;
    public APIsGuruSearchProvider(Uri searchUri, HttpClient httpClient, ILogger logger, bool clearCache)
    {
        ArgumentNullException.ThrowIfNull(searchUri);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);
        cachingProvider = new DocumentCachingProvider{
            HttpClient = httpClient,
            Logger = logger,
            ClearCache = clearCache,
        };
        SearchUri = searchUri;
    }
    public async Task<IEnumerable<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        if (SearchUri == null)
            return Enumerable.Empty<SearchResult>();
        using var rawDocument = await cachingProvider.GetDocumentAsync(SearchUri, "search", "apisguru.json", cancellationToken);
        var apiEntries = JsonSerializer.Deserialize<Dictionary<string, APIEntry>>(rawDocument);
        var results = apiEntries.Where(x => x.Key.Contains(query, StringComparison.OrdinalIgnoreCase))
                                .Select(static x => x.Value.versions.TryGetValue(x.Value.preferred, out var version) ? (x.Key, version) : (x.Key, default))
                                .Where(static x => x.version is not null)
                                .Select(x => new SearchResult(x.Key, x.version.info?.title, x.version.info?.description, x.version.info?.contact?.url, x.version.info?.origin?.FirstOrDefault()?.url));
        return results;
    }
}
