using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.SearchProviders;
using Kiota.Builder.SearchProviders.APIsGuru;
using Kiota.Builder.SearchProviders.GitHub;
using Kiota.Builder.SearchProviders.MSGraph;
using Microsoft.Extensions.Logging;

namespace Kiota.Builder;

public class KiotaSearcher {
    private readonly ILogger<KiotaSearcher> logger;
    private readonly SearchConfiguration config;
    private readonly HttpClient httpClient;
    public KiotaSearcher(ILogger<KiotaSearcher> logger, SearchConfiguration config, HttpClient httpClient) {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(httpClient);
        this.logger = logger;
        this.config = config;
        this.httpClient = httpClient;
    }
    public async Task<IDictionary<string, SearchResult>> SearchAsync(CancellationToken cancellationToken) {
        if (string.IsNullOrEmpty(config.SearchTerm)) {
            logger.LogError("no search term provided");
            return new Dictionary<string, SearchResult>();
        }
        var apiGurusSearchProvider = new APIsGuruSearchProvider(config.APIsGuruListUrl, httpClient, logger, config.ClearCache);
        logger.LogDebug("searching for {searchTerm}", config.SearchTerm);
        logger.LogDebug("searching APIs.guru with url {url}", config.APIsGuruListUrl);
        var oasProvider = new OpenApiSpecSearchProvider();
        var githubProvider = new GitHubSearchProvider(httpClient, logger, config.ClearCache, config.GitHub);
        var results = await Task.WhenAll(
                        SearchProviderAsync(apiGurusSearchProvider, cancellationToken),
                        SearchProviderAsync(oasProvider, cancellationToken),
                        SearchProviderAsync(githubProvider, cancellationToken));
        return results.SelectMany(static x => x)
                .ToDictionary(static x => x.Key, static x => x.Value, StringComparer.OrdinalIgnoreCase);
    }
    private async Task<IDictionary<string, SearchResult>> SearchProviderAsync(ISearchProvider provider, CancellationToken cancellationToken) {
        var providerPrefix = $"{provider.ProviderKey}{ProviderSeparator}";
        var results = await provider.SearchAsync(config.SearchTerm.Replace(providerPrefix, string.Empty), config.Version, cancellationToken);

        return results.Select(x => ($"{providerPrefix}{x.Key}", x.Value))
                    .ToDictionary(static x => x.Item1, static x => x.Value, StringComparer.OrdinalIgnoreCase);
    }
    public const string ProviderSeparator = "::";
}
