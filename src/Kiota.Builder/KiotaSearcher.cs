using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.SearchProviders;
using Kiota.Builder.SearchProviders.APIsGuru;
using Microsoft.Extensions.Logging;

namespace Kiota.Builder;

public class KiotaSearcher {
    private readonly ILogger<KiotaSearcher> logger;
    private readonly SearchConfiguration config;
    public KiotaSearcher(ILogger<KiotaSearcher> logger, SearchConfiguration config) {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(config);
        this.logger = logger;
        this.config = config;
    }
    public async Task<IDictionary<string, SearchResult>> SearchAsync(CancellationToken cancellationToken) {
        if (string.IsNullOrEmpty(config.SearchTerm)) {
            logger.LogError("no search term provided");
            return new Dictionary<string, SearchResult>();
        }
        using var client = new HttpClient();
        var apiGurusSearchProvider = new APIsGuruSearchProvider(config.APIsGuruListUrl, client, logger, config.ClearCache);
        logger.LogDebug("searching for {searchTerm}", config.SearchTerm);
        logger.LogDebug("searching APIs.guru with url {url}", config.APIsGuruListUrl);
        var providerPrefix = $"{apiGurusSearchProvider.ProviderKey}{ProviderSeparator}";
        var results = await apiGurusSearchProvider.SearchAsync(config.SearchTerm.Replace(providerPrefix, string.Empty), cancellationToken);

        return results.Select(x => ($"{providerPrefix}{x.Key}", x.Value))
                    .ToDictionary(static x => x.Item1, static x => x.Value, StringComparer.OrdinalIgnoreCase);
    }
    internal const string ProviderSeparator = "::";
}
