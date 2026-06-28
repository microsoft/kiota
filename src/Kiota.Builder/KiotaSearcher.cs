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

public partial class KiotaSearcher
{
    private readonly ILogger<KiotaSearcher> _logger;
    private readonly SearchConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly IAuthenticationProvider? _gitHubAuthenticationProvider;
    private readonly Func<CancellationToken, Task<bool>> _isGitHubSignedInCallBack;

    public KiotaSearcher(ILogger<KiotaSearcher> logger, SearchConfiguration config, HttpClient httpClient, IAuthenticationProvider? gitHubAuthenticationProvider, Func<CancellationToken, Task<bool>> isGitHubSignedInCallBack)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(httpClient);
        _logger = logger;
        _config = config;
        _httpClient = httpClient;
        _gitHubAuthenticationProvider = gitHubAuthenticationProvider;
        _isGitHubSignedInCallBack = isGitHubSignedInCallBack;
    }
    public async Task<IDictionary<string, SearchResult>> SearchAsync(string? searchTerm, string? version, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            LogNoSearchTermProvided();
            return new Dictionary<string, SearchResult>();
        }
        var apiGurusSearchProvider = new APIsGuruSearchProvider(_config.APIsGuruListUrl, _httpClient, _logger, _config.ClearCache);
        LogSearchingFor(searchTerm);
        LogSearchingApisGuru(_config.APIsGuruListUrl);
        var oasProvider = new OpenApiSpecSearchProvider();
        var githubProvider = new GitHubSearchProvider(_httpClient, _logger, _config.ClearCache, _config.GitHub, _gitHubAuthenticationProvider, _isGitHubSignedInCallBack);
        var results = await Task.WhenAll(
                        SearchProviderAsync(searchTerm, version, apiGurusSearchProvider, cancellationToken),
                        SearchProviderAsync(searchTerm, version, oasProvider, cancellationToken),
                        SearchProviderAsync(searchTerm, version, githubProvider, cancellationToken)).ConfigureAwait(false);
        return results.SelectMany(static x => x)
                .ToDictionary(static x => x.Key, static x => x.Value, StringComparer.OrdinalIgnoreCase);
    }
    private static async Task<IDictionary<string, SearchResult>> SearchProviderAsync(string searchTerm, string? version, ISearchProvider provider, CancellationToken cancellationToken)
    {
        var providerPrefix = $"{provider.ProviderKey}{ProviderSeparator}";
        var results = await provider.SearchAsync(searchTerm.Replace(providerPrefix, string.Empty, StringComparison.OrdinalIgnoreCase), version, cancellationToken).ConfigureAwait(false);

        return results
                    .Where(static x => x.Value.DescriptionUrl is not null)
                    .Select(x => ($"{providerPrefix}{x.Key}", x.Value))
                    .ToDictionary(static x => x.Item1, static x => x.Value, StringComparer.OrdinalIgnoreCase);
    }
    public const string ProviderSeparator = "::";

    [LoggerMessage(Level = LogLevel.Error, Message = "no search term provided")]
    private partial void LogNoSearchTermProvided();

    [LoggerMessage(Level = LogLevel.Debug, Message = "searching for {SearchTerm}")]
    private partial void LogSearchingFor(string searchTerm);

    [LoggerMessage(Level = LogLevel.Debug, Message = "searching APIs.guru with url {Url}")]
    private partial void LogSearchingApisGuru(Uri url);
}
