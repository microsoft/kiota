using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System;
using System.Text.Json;
using System.Linq;
using Microsoft.Extensions.Logging;
using Kiota.Builder.Caching;

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
    public string ProviderKey => "apisguru";
    public HashSet<string> KeysToExclude { get; set; } = new() {
        "microsoft.com:graph"
    };
    public async Task<IDictionary<string, SearchResult>> SearchAsync(string query, string queryVersion, CancellationToken cancellationToken)
    {
        if (SearchUri == null)
            return new Dictionary<string, SearchResult>();
        using var rawDocument = await cachingProvider.GetDocumentAsync(SearchUri, "search", "apisguru.json", cancellationToken);
        var apiEntries = JsonSerializer.Deserialize<Dictionary<string, APIEntry>>(rawDocument);
        var candidates = apiEntries
                            .Where(x => !KeysToExclude.Contains(x.Key))
                            .Where(x => x.Key.Contains(query, StringComparison.OrdinalIgnoreCase));
        var singleCandidate = !string.IsNullOrEmpty(queryVersion) && candidates.Count() == 1;
        var results = candidates
                                .Select(x => x.Value.versions.TryGetValue(singleCandidate ? queryVersion : x.Value.preferred, out var version) ? (x.Key, version, x.Value.versions.Keys.ToList()) : (x.Key, default, default))
                                .Where(static x => x.version is not null)
                                .ToDictionary(static x => x.Key,
                                            static x => new SearchResult(x.version.info?.title, x.version.info?.description, x.version.info?.contact?.url, x.version.info?.origin?.FirstOrDefault()?.url, x.Item3),
                                            StringComparer.OrdinalIgnoreCase);
        return results;
    }
}
