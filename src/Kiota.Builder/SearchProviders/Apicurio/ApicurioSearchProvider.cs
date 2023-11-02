

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ApiSdk;
using ApiSdk.Models;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Kiota.Builder.SearchProviders.Apicurio;

public class ApicurioSearchProvider : ISearchProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly IAuthenticationProvider? _authenticatedAuthenticationProvider;
    private readonly ApicurioConfiguration _configuration;

    public string ProviderKey => "apicurio";

    public HashSet<string> KeysToExclude
    {
        get; init;
    } = new();

    public ApicurioSearchProvider(HttpClient httpClient, ILogger logger, bool clearCache, ApicurioConfiguration configuration, IAuthenticationProvider? authenticatedAuthenticationProvider)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _authenticatedAuthenticationProvider = authenticatedAuthenticationProvider;
        KeysToExclude = new(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IDictionary<string, SearchResult>> SearchAsync(string term, string? version, CancellationToken cancellationToken)
    {
        var authenticationProvider = _authenticatedAuthenticationProvider != null ?
            _authenticatedAuthenticationProvider :
            new AnonymousAuthenticationProvider();
        using var requestAdapter = new HttpClientRequestAdapter(authenticationProvider, httpClient: _httpClient);
        requestAdapter.BaseUrl = _configuration.ApiBaseUrl.AbsoluteUri;
        var apicurioClient = new ApicurioClient(requestAdapter);

        ArtifactSearchResults? results;
        try
        {

            // TODO search also for versions
            results = await apicurioClient.Search.Artifacts.GetAsync(config =>
            {
                config.QueryParameters.Limit = 100;
                config.QueryParameters.Offset = 0;
                config.QueryParameters.Labels = new string[] { term };
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            _logger.LogWarning("Error connecting to Apicurio Registry at the URL {String}", _configuration.ApiBaseUrl);
            return new Dictionary<string, SearchResult>();
        }

        if (results == null)
            return new Dictionary<string, SearchResult>();

        return results.Artifacts!.Select(x =>
        {
            var groupId = (x.GroupId != null) ? x.GroupId : "default";
            // TODO: FIXME this is wrong
            var baseUrl = new Uri(_configuration.ApiBaseUrl.AbsoluteUri.Replace("apis\\/registry\\/v2\\/", string.Empty, StringComparison.OrdinalIgnoreCase) + "/ui/artifacts/" + groupId + "/" + x.Id);

            return new Tuple<string, SearchResult>(term,
                new SearchResult(x.Name ?? string.Empty,
                    x.Description ?? string.Empty,
                    baseUrl,
                    new Uri(_configuration.ApiBaseUrl + "/groups/" + groupId + "/artifacts/" + x.Id),
                    new()));
        }).DistinctBy(static x => x.Item1, StringComparer.OrdinalIgnoreCase)
                                .ToDictionary(static x => x.Item1,
                                            static x => x.Item2,
                                            StringComparer.OrdinalIgnoreCase);
    }
}
