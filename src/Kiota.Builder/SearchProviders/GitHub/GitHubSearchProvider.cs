﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Caching;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models;
using Kiota.Builder.SearchProviders.GitHub.Index;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using SharpYaml;
using YamlDotNet.Serialization;

namespace Kiota.Builder.SearchProviders.GitHub;

public class GitHubSearchProvider : ISearchProvider
{
    private readonly DocumentCachingProvider documentCachingProvider;
    private readonly ILogger _logger;
    private readonly Uri _blockListUrl;
    private readonly IAuthenticationProvider? _authenticatedAuthenticationProvider;
    private readonly Func<CancellationToken, Task<bool>> _isSignedInCallback;
    public GitHubSearchProvider(HttpClient httpClient, ILogger logger, bool clearCache, GitHubConfiguration configuration, IAuthenticationProvider? authenticatedAuthenticationProvider, Func<CancellationToken, Task<bool>> isSignedInCallBack)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configuration.BlockListUrl);
        ArgumentNullException.ThrowIfNull(logger);
        documentCachingProvider = new DocumentCachingProvider(httpClient, logger)
        {
            ClearCache = clearCache,
        };
        _httpClient = httpClient;
        _logger = logger;
        _blockListUrl = configuration.BlockListUrl;
        _authenticatedAuthenticationProvider = authenticatedAuthenticationProvider;
        _isSignedInCallback = isSignedInCallBack;
        KeysToExclude = new(StringComparer.OrdinalIgnoreCase);
    }
    private readonly HttpClient _httpClient;
    public string ProviderKey => "github";
    private readonly HashSet<string> _topics = new(StringComparer.OrdinalIgnoreCase) {
        "openapi-index",
        "kiota-index"
    };
    private readonly Dictionary<string, string> _indexFileInfos = new(StringComparer.OrdinalIgnoreCase) {
        {"apis.yaml", "text/yaml"},
        {"apis.json", "application/json"}
    };
    public HashSet<string> KeysToExclude
    {
        get; init;
    }
    public Task<IDictionary<string, SearchResult>> SearchAsync(string term, string? version, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(term);
        return SearchAsyncInternalAsync(term, cancellationToken);
    }
    private static bool BlockListContainsRepo(Tuple<HashSet<string>, HashSet<string>> blockLists, string? organization, string? repo) =>
        !string.IsNullOrEmpty(organization) && blockLists.Item1.Contains(organization) || blockLists.Item2.Contains($"{organization}/{repo}");
    private async Task<IDictionary<string, SearchResult>> SearchAsyncInternalAsync(string term, CancellationToken cancellationToken)
    {
        var blockLists = await GetBlockListsAsync(cancellationToken).ConfigureAwait(false);
        var isSignedIn = _isSignedInCallback != null && await _isSignedInCallback(cancellationToken).ConfigureAwait(false);
        var authenticationProvider = _authenticatedAuthenticationProvider != null && isSignedIn ?
            _authenticatedAuthenticationProvider :
            new Authentication.AnonymousAuthenticationProvider();
        using var gitHubRequestAdapter = new HttpClientRequestAdapter(authenticationProvider, httpClient: _httpClient);
        var gitHubClient = new GitHubClient.GitHubClient(gitHubRequestAdapter);
        if (term.Contains('/', StringComparison.OrdinalIgnoreCase))
        {
            var parts = term.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var owner = parts[0];
            var repo = parts[1];
            if (!BlockListContainsRepo(blockLists, owner, repo))
            {
                var keyResults = GetDictionaryResultFromMultipleSources(await Task.WhenAll(_indexFileInfos.Select(x => GetSearchResultsFromRepoAsync(gitHubClient, owner, repo, x.Key, x.Value, cancellationToken))).ConfigureAwait(false));
                if (parts.Length > 2 && keyResults.TryGetValue(term, out var result))
                    return new Dictionary<string, SearchResult>(StringComparer.OrdinalIgnoreCase) { { term, result } };
                else if (keyResults.Count != 0)
                    return keyResults;
            }
        }

        var results = (await Task.WhenAll(_topics.Select(x => GetAllReposForTermAsync(gitHubClient, term, x, cancellationToken)))
                                .ConfigureAwait(false))
                        .SelectMany(static x => x)
                        .Where(x => x is not null && !BlockListContainsRepo(blockLists, x.Owner?.Login, x.Name))
                        .DistinctBy(static x => x.Url, StringComparer.OrdinalIgnoreCase)
                        .ToList();

        var searchResults = GetDictionaryResultFromMultipleSources(await Task.WhenAll(results.Join(_indexFileInfos, x => true, x => true, (repo, indexFileInfo) => (repo, indexFileInfo))
                                                    .Select(x => GetSearchResultsFromRepoAsync(gitHubClient, x.repo.Owner?.Login, x.repo.Name, x.indexFileInfo.Key, x.indexFileInfo.Value, cancellationToken))).ConfigureAwait(false));

        return searchResults;
    }
    private static Dictionary<string, SearchResult> GetDictionaryResultFromMultipleSources(IEnumerable<IEnumerable<Tuple<string, SearchResult>>> sources) =>
        sources.SelectMany(static x => x)
                .DistinctBy(static x => x.Item1, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(static x => x.Item1, static x => x.Item2, StringComparer.OrdinalIgnoreCase);
    private async Task<Tuple<HashSet<string>, HashSet<string>>> GetBlockListsAsync(CancellationToken cancellationToken)
    {
        try
        {
#pragma warning disable CA2007
            await using var document = await documentCachingProvider.GetDocumentAsync(_blockListUrl, "search", _blockListUrl.GetFileName(), "text/yaml", cancellationToken).ConfigureAwait(false);
#pragma warning restore CA2007
            var deserialized = deserializeDocumentFromYaml<BlockList>(document);
            return new Tuple<HashSet<string>, HashSet<string>>(
                new HashSet<string>(deserialized.Organizations.Distinct(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(deserialized.Repositories.Distinct(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase));
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _logger.LogError(ex, "Error while getting block list");
            return new Tuple<HashSet<string>, HashSet<string>>(new HashSet<string>(StringComparer.OrdinalIgnoreCase), new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }
    }
    private const string OpenApiPropertyKey = "X-openapi";
    private static readonly Lazy<IDeserializer> _deserializer = new(() => new DeserializerBuilder()
                .WithNamingConvention(new YamlNamingConvention())
                .IgnoreUnmatchedProperties()
                .Build());
    private static async Task<IndexRoot?> deserializeDocumentFromJsonAsync(Stream document, CancellationToken cancellationToken) => await JsonSerializer.DeserializeAsync(document, indexRootContext.IndexRoot, cancellationToken).ConfigureAwait(false);
    private static readonly IndexRootJsonContext indexRootContext = new(new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
    });
    private static T deserializeDocumentFromYaml<T>(Stream document)
    {
        using var reader = new StreamReader(document);
        return _deserializer.Value.Deserialize<T>(reader);
    }
    private async Task<IEnumerable<Tuple<string, SearchResult>>> GetSearchResultsFromRepoAsync(GitHubClient.GitHubClient gitHubClient, string? org, string? repo, string fileName, string accept, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(org) || string.IsNullOrEmpty(repo))
            return [];
        try
        {
            if (await gitHubClient.Repos[org][repo].Contents[fileName].GetAsync(cancellationToken: cancellationToken).ConfigureAwait(false)
                 is not { ContentFile.DownloadUrl: string downloadUrl } || string.IsNullOrEmpty(downloadUrl))
                return [];
            var targetUrl = new Uri(downloadUrl);
#pragma warning disable CA2007
            await using var document = await documentCachingProvider.GetDocumentAsync(targetUrl, "search", targetUrl.GetFileName(), accept, cancellationToken).ConfigureAwait(false);
#pragma warning restore CA2007
            var indexFile = accept.ToLowerInvariant() switch
            {
                "application/json" => await deserializeDocumentFromJsonAsync(document, cancellationToken).ConfigureAwait(false),
                "text/yaml" => deserializeDocumentFromYaml<IndexRoot>(document),
                _ => throw new InvalidOperationException($"Unsupported accept type {accept}"),
            };
            if (indexFile is null || indexFile.Apis is null)
                return [];
            await GetUrlForRelativeDescriptionsAsync(indexFile.Apis, gitHubClient, org, repo, cancellationToken).ConfigureAwait(false);
            var results = indexFile.Apis.Where(static x => x.Properties.Any(static y => y.Type.Equals(OpenApiPropertyKey, StringComparison.OrdinalIgnoreCase)))
                                .Select(x =>
                                {
                                    var baseUrl = string.IsNullOrEmpty(x.BaseURL) ? null : new Uri(x.BaseURL);
                                    var hostAndPath = baseUrl == null ? string.Empty : $"/{baseUrl.Host}{baseUrl.AbsolutePath}";
                                    var property = x.Properties.FirstOrDefault(y => OpenApiPropertyKey.Equals(y.Type, StringComparison.OrdinalIgnoreCase));
                                    return new Tuple<string, SearchResult>($"{org}/{repo}{hostAndPath}",
                                        new SearchResult(x.Name,
                                            x.Description,
                                            baseUrl,
                                            string.IsNullOrEmpty(property?.Url) ? null : new Uri(property!.Url),
                                            new()));
                                })
                                .ToList();
            return results;
        }
        catch (BasicError)
        {
            _logger.LogInformation("Unable to find {FileName} in {Org}/{Repo}", fileName, org, repo);
        }
        catch (Exception ex) when (ex is YamlException || ex is JsonException)
        {
#if DEBUG
            _logger.LogError(ex, "Error while parsing the file {FileName} in {Org}/{Repo}", fileName, org, repo);
#else
            _logger.LogInformation("Error while parsing the file {FileName} in {Org}/{Repo}", fileName, org, repo);
#endif
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error while downloading the file {FileName} in {Org}/{Repo}", fileName, org, repo);
        }
        return [];
    }
    private async Task GetUrlForRelativeDescriptionsAsync(List<IndexApiEntry> originalResults, GitHubClient.GitHubClient gitHubClient, string org, string repo, CancellationToken cancellationToken)
    {
        var relativeUrlsResults = originalResults.Where(static x => x.Properties.Any(static y => y.Type.Equals(OpenApiPropertyKey, StringComparison.OrdinalIgnoreCase) && !y.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase)));
        if (!relativeUrlsResults.Any())
            return;
        var resultsToUpdate = await Task.WhenAll(relativeUrlsResults.Select(x => GetUrlForRelativeDescriptionAsync(x, gitHubClient, org, repo, cancellationToken))).ConfigureAwait(false);
        var keysToRemove = resultsToUpdate.Where(static x => x.Item2 is null).Select(static x => x.Item1).ToHashSet(StringComparer.OrdinalIgnoreCase);
        originalResults.RemoveAll(x => x.Properties.Any(y => y.Type.Equals(OpenApiPropertyKey, StringComparison.OrdinalIgnoreCase) && keysToRemove.Contains(y.Url)));
        resultsToUpdate.Where(static x => x.Item2 is not null).ToList().ForEach(x =>
        {
            var resultToUpdate = originalResults.FirstOrDefault(z => z.Properties.Any(y => y.Type.Equals(OpenApiPropertyKey, StringComparison.OrdinalIgnoreCase) && x.Item1.Equals(y.Url, StringComparison.OrdinalIgnoreCase)));
            if (resultToUpdate?.Properties.FirstOrDefault(static y => y.Type.Equals(OpenApiPropertyKey, StringComparison.OrdinalIgnoreCase)) is IndexApiProperty propertyToUpdate &&
                !string.IsNullOrEmpty(x.Item2))
                propertyToUpdate.Url = x.Item2;
        });
    }
    private async Task<Tuple<string, string?>> GetUrlForRelativeDescriptionAsync(IndexApiEntry searchResult, GitHubClient.GitHubClient gitHubClient, string org, string repo, CancellationToken cancellationToken)
    {
        var originalUrl = searchResult.Properties.First(static y => y.Type.Equals(OpenApiPropertyKey, StringComparison.OrdinalIgnoreCase)).Url;
        try
        {
            var fileName = originalUrl.TrimStart('/');
            if (await gitHubClient.Repos[org][repo].Contents[fileName].GetAsync(cancellationToken: cancellationToken).ConfigureAwait(false) is
                { ContentFile.DownloadUrl: string downloadUrl } && !string.IsNullOrEmpty(downloadUrl))
                return new Tuple<string, string?>(originalUrl, downloadUrl);
        }
        catch (BasicError)
        {
            _logger.LogInformation("Unable to find {FileName} in {Org}/{Repo}", originalUrl, org, repo);
        }
        return new Tuple<string, string?>(originalUrl, null);
    }
    private async Task<List<RepoSearchResultItem>> GetAllReposForTermAsync(GitHubClient.GitHubClient gitHubClient, string term, string topic, CancellationToken cancellationToken)
    {
        var results = new List<RepoSearchResultItem>();
        var shouldContinue = false;
        var pageNumber = 1;
        do
        {
            var reposPage = await gitHubClient.Search.Repositories.GetAsync(x =>
            {
                x.QueryParameters.Q = $"{term} topic:{topic} fork:true";
                x.QueryParameters.Page = pageNumber;
                _logger.LogTrace("Page {PageNumber}", x.QueryParameters.Page); // using the property is intentional to avoid trimming
                _logger.LogTrace("Query: {Query}", x.QueryParameters.Q);
            }, cancellationToken).ConfigureAwait(false);
            if (reposPage == null)
                break;
            if (reposPage.Items != null)
                results.AddRange(reposPage.Items);
            shouldContinue = results.Count < reposPage.TotalCount;
            pageNumber++;
        } while (shouldContinue);
        return results;
    }
}
