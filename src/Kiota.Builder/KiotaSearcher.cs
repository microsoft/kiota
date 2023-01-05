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
using Microsoft.Kiota.Abstractions.Authentication;

namespace Kiota.Builder;

public class KiotaSearcher {
    private readonly ILogger<KiotaSearcher> _logger;
    private readonly SearchConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly IAuthenticationProvider _gitHubAuthenticationProvider;
    private readonly Func<CancellationToken, Task<bool>> _isGitHubSignedInCallBack;

    public KiotaSearcher(ILogger<KiotaSearcher> logger, SearchConfiguration config, HttpClient httpClient, IAuthenticationProvider gitHubAuthenticationProvider, Func<CancellationToken, Task<bool>> isGitHubSignedInCallBack) {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(httpClient);
        _logger = logger;
        _config = config;
        _httpClient = httpClient;
        _gitHubAuthenticationProvider = gitHubAuthenticationProvider;
        _isGitHubSignedInCallBack = isGitHubSignedInCallBack;
    }
    public async Task<IDictionary<string, SearchResult>> SearchAsync(string searchTerm, string version, CancellationToken cancellationToken) {
        if (string.IsNullOrEmpty(searchTerm)) {
            _logger.LogError("no search term provided");
            return new Dictionary<string, SearchResult>();
        }
        var apiGurusSearchProvider = new APIsGuruSearchProvider(_config.APIsGuruListUrl, _httpClient, _logger, _config.ClearCache);
        _logger.LogDebug("searching for {searchTerm}", searchTerm);
        _logger.LogDebug("searching APIs.guru with url {url}", _config.APIsGuruListUrl);
        var oasProvider = new OpenApiSpecSearchProvider();
        var githubProvider = new GitHubSearchProvider(_httpClient, _logger, _config.ClearCache, _config.GitHub, _gitHubAuthenticationProvider, _isGitHubSignedInCallBack);
        var results = await Task.WhenAll(
                        SearchProviderAsync(searchTerm, version, apiGurusSearchProvider, cancellationToken),
                        SearchProviderAsync(searchTerm, version, oasProvider, cancellationToken),
                        SearchProviderAsync(searchTerm, version, githubProvider, cancellationToken));
        return results.SelectMany(static x => x)
                .ToDictionary(static x => x.Key, static x => x.Value, StringComparer.OrdinalIgnoreCase);
    }
    private static async Task<IDictionary<string, SearchResult>> SearchProviderAsync(string searchTerm, string version, ISearchProvider provider, CancellationToken cancellationToken) {
        var providerPrefix = $"{provider.ProviderKey}{ProviderSeparator}";
        var results = await provider.SearchAsync(searchTerm.Replace(providerPrefix, string.Empty), version, cancellationToken);

        return results.Select(x => ($"{providerPrefix}{x.Key}", x.Value))
                    .ToDictionary(static x => x.Item1, static x => x.Value, StringComparer.OrdinalIgnoreCase);
    }
    public const string ProviderSeparator = "::";
}
