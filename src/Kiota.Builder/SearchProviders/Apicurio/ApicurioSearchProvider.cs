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

    private readonly static IDictionary<string, SearchResult> EMPTY_RESULT = new Dictionary<string, SearchResult>();

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
        if (_configuration.ApiBaseUrl == null)
        {
            _logger.LogInformation("Apicurio provider not configured, skipping.");
            return EMPTY_RESULT;
        }

        var authenticationProvider = _authenticatedAuthenticationProvider != null ?
            _authenticatedAuthenticationProvider :
            new AnonymousAuthenticationProvider();
        using var requestAdapter = new HttpClientRequestAdapter(authenticationProvider, httpClient: _httpClient);
        requestAdapter.BaseUrl = _configuration.ApiBaseUrl.AbsoluteUri;
        var apicurioClient = new ApicurioClient(requestAdapter);

        IDictionary<string, SearchResult> result;
        try
        {
            ArtifactSearchResults? searchResults = await apicurioClient.Search.Artifacts.GetAsync(config =>
            {
                config.QueryParameters.Limit = _configuration.ArtifactsLimit;
                config.QueryParameters.Offset = 0;
                switch (_configuration.SearchBy)
                {
                    case ApicurioConfiguration.ApicurioSearchBy.LABEL:
                        config.QueryParameters.Labels = new string[] { term };
                        break;
                    case ApicurioConfiguration.ApicurioSearchBy.PROPERTY:
                        config.QueryParameters.Properties = new string[] { term };
                        break;
                }
            }, cancellationToken).ConfigureAwait(false);

            if (searchResults == null || searchResults!.Artifacts == null)
                return EMPTY_RESULT;

            var dictionaries = searchResults!.Artifacts!.Select(async x =>
            {
                var groupId = (x.GroupId != null) ? x.GroupId : "default";
                var uiUrl = new Uri(_configuration.UIBaseUrl + "/artifacts/" + groupId + "/" + x.Id);
                var restUrl = new Uri(_configuration.ApiBaseUrl + "/groups/" + groupId + "/artifacts/" + x.Id);

                if (!string.IsNullOrEmpty(version))
                {
                    var versionMetadata = await apicurioClient.Groups[groupId].Artifacts[x.Id].Versions[version!].Meta.GetAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (versionMetadata == null || versionMetadata!.Version == null)
                    {
                        return EMPTY_RESULT;
                    }

                    return new Dictionary<string, SearchResult>()
                    {
                        [x.Id!] = new SearchResult(x.Name ?? x.Id!,
                            x.Description ?? string.Empty,
                            uiUrl,
                            restUrl,
                            new List<string>(1) { versionMetadata!.Version! })
                    };
                }
                else
                {
                    var versions = await apicurioClient.Groups[groupId].Artifacts[x.Id].Versions.GetAsync(config =>
                    {
                        config.QueryParameters.Limit = _configuration.VersionsLimit;
                        config.QueryParameters.Offset = 0;
                    }, cancellationToken).ConfigureAwait(false);

                    if (versions == null || versions.Versions == null)
                        return EMPTY_RESULT;

                    return new Dictionary<string, SearchResult>()
                    {
                        [x.Id!] = new SearchResult(x.Name ?? x.Id!,
                            x.Description ?? string.Empty,
                            uiUrl,
                            restUrl,
                            versions.Versions!.Select(static v => v.Version!).ToList())
                    };
                }
            });

            var x = await Task.WhenAll(dictionaries).ConfigureAwait(false);
            result = x.SelectMany(static dict => dict).ToDictionary(static x => x.Key, static x => x.Value, StringComparer.OrdinalIgnoreCase);
        }
        catch (HttpRequestException)
        {
            _logger.LogWarning("Error connecting to Apicurio Registry at the URL {String}", _configuration.ApiBaseUrl);
            return EMPTY_RESULT;
        }
        catch (ApiSdk.Models.Error)
        {
            return EMPTY_RESULT;
        }

        if (result == null)
            return EMPTY_RESULT;

        return result;
    }
}
