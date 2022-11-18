using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Caching;
using Kiota.Builder.SearchProviders.GitHub.Authentication;
using Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models;
using Kiota.Builder.SearchProviders.GitHub.Index;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Kiota.Builder.SearchProviders.GitHub;

public class GitHubSearchProvider : ISearchProvider
{
    private readonly DocumentCachingProvider cachingProvider;
    public GitHubSearchProvider(HttpClient httpClient, ILogger logger, bool clearCache)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);
        cachingProvider = new DocumentCachingProvider(httpClient, logger) {
            ClearCache = clearCache,
        };
        _httpClient = httpClient;
    }
    private readonly HttpClient _httpClient;
    public string ProviderKey => "github";
    private readonly HashSet<string> _topics = new(StringComparer.OrdinalIgnoreCase) {
        "openapi-index",
        "kiota-index"
    };
    private readonly HashSet<string> _indexFileNames = new(StringComparer.OrdinalIgnoreCase) {
        "apis.yaml",
        "apis.json"
    };
    public HashSet<string> KeysToExclude { get; set; }
    public async Task<IDictionary<string, SearchResult>> SearchAsync(string term, string version, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(term))
            throw new ArgumentNullException(nameof(term));

        //TODO set an exclusion mechanism for the search provider that's based off repos URLS
        var gitHubRequestAdapter = new HttpClientRequestAdapter(new GitHubAnonymousAuthenticationProvider(), httpClient: _httpClient);
        var gitHubClient = new GitHubClient.GitHubClient(gitHubRequestAdapter);
        var results = (await Task.WhenAll(_topics.Select(x => GetAllReposForTerm(gitHubClient, term, x, cancellationToken)))
                                .ConfigureAwait(false))
                        .SelectMany(static x => x)
                        .DistinctBy(static x => x.Url, StringComparer.OrdinalIgnoreCase)
                        .ToList();
        
        var searchResults = (await Task.WhenAll(results.Join(_indexFileNames, static x => true, static x => true, (repo, indexFileName) => (repo, indexFileName))
                                                    .Select(x => GetSearchResultsFromRepo(gitHubClient, x.repo, x.indexFileName, cancellationToken))).ConfigureAwait(false))
                                .SelectMany(static x => x)
                                .DistinctBy(static x => x.Item1, StringComparer.OrdinalIgnoreCase)
                                .ToDictionary(static x => x.Item1, static x => x.Item2, StringComparer.OrdinalIgnoreCase);
        
        return searchResults; 
    }
    private const string OpenApiPropertyKey = "x-openapi";
    private async Task<IEnumerable<Tuple<string, SearchResult>>> GetSearchResultsFromRepo(GitHubClient.GitHubClient gitHubClient, RepoSearchResultItem repo, string fileName, CancellationToken cancellationToken) {
        try {
            var response = await gitHubClient.Repos[repo.Owner.Login][repo.Name].Contents[fileName].GetAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!response.AdditionalData.TryGetValue("download_url", out var rawDownloadUrl) || rawDownloadUrl is not string downloadUrl || string.IsNullOrEmpty(downloadUrl))
                return Enumerable.Empty<Tuple<string, SearchResult>>();
            await using var document = await cachingProvider.GetDocumentAsync(new Uri(downloadUrl), "search", Path.GetFileName(downloadUrl), cancellationToken);
            var indexFile = JsonSerializer.Deserialize<IndexRoot>(document, new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true,
            });//TODO handle YAML
            if (indexFile is null || indexFile.Apis is null)
                return Enumerable.Empty<Tuple<string, SearchResult>>();
            return indexFile.Apis.Where(static x => x.Properties.Any(static y => y.Type.Equals(OpenApiPropertyKey, StringComparison.OrdinalIgnoreCase)))
                                .Select(x => {
                                        var baseUrl = new Uri(x.BaseUrl);
                                        var hostAndPath = baseUrl.Host + baseUrl.AbsolutePath;
                                        return new Tuple<string, SearchResult>($"{repo.Owner.Login}/{repo.Name}/{hostAndPath}",
                                            new SearchResult(x.Name,
                                                x.Description,
                                                new Uri(x.BaseUrl),
                                                new Uri(x.Properties.FirstOrDefault(y => OpenApiPropertyKey.Equals(y.Type, StringComparison.OrdinalIgnoreCase))?.Url),
                                                new()));
                                        });
        } catch (BasicError) {
            // we couldn't find the file, we'll just ignore it
            return Enumerable.Empty<Tuple<string, SearchResult>>();
        }
    }
    private static async Task<List<RepoSearchResultItem>> GetAllReposForTerm(GitHubClient.GitHubClient gitHubClient, string term, string topic, CancellationToken cancellationToken) {
        var results = new List<RepoSearchResultItem>();
        var shouldContinue = false;
        var pageNumber = 1;
        do {
            var reposPage = await gitHubClient.Search.Repositories.GetAsync(x => {
                x.QueryParameters.Q = $"{term} topic:{topic}";
                x.QueryParameters.Page = pageNumber;
            }, cancellationToken).ConfigureAwait(false);
            results.AddRange(reposPage.Items);
            shouldContinue = results.Count < reposPage.Total_count;
            pageNumber++;
        } while(shouldContinue);
        return results;
    }
}
