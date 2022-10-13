using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.SearchProviders;
using Kiota.Builder.SearchProviders.APIsGuru;
using Kiota.Builder.SearchProviders.MSGraph;
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
        var msGraphProvider = new MSGraphSearchProvider();
        var oasProvider = new OpenApiSpecSearchProvider();
        var results = await Task.WhenAll(
                        SearchProviderAsync(apiGurusSearchProvider, cancellationToken),
                        SearchProviderAsync(msGraphProvider, cancellationToken),
                        SearchProviderAsync(oasProvider, cancellationToken));
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
