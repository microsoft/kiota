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
using SharpYaml;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Kiota.Builder.SearchProviders.GitHub;

public class GitHubSearchProvider : ISearchProvider
{
    private readonly DocumentCachingProvider cachingProvider;
    private readonly ILogger _logger;
    public GitHubSearchProvider(HttpClient httpClient, ILogger logger, bool clearCache)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);
        cachingProvider = new DocumentCachingProvider(httpClient, logger)
        {
            ClearCache = clearCache,
        };
        _httpClient = httpClient;
        _logger = logger;
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
        get; set;
    }
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

        var searchResults = (await Task.WhenAll(results.Join(_indexFileInfos, x => true, x => true, (repo, indexFileInfo) => (repo, indexFileInfo))
                                                    .Select(x => GetSearchResultsFromRepo(gitHubClient, x.repo, x.indexFileInfo.Key, x.indexFileInfo.Value, cancellationToken))).ConfigureAwait(false))
                                .SelectMany(static x => x)
                                .DistinctBy(static x => x.Item1, StringComparer.OrdinalIgnoreCase)
                                .ToDictionary(static x => x.Item1, static x => x.Item2, StringComparer.OrdinalIgnoreCase);

        return searchResults;
    }
    private const string OpenApiPropertyKey = "x-openapi";
    private static readonly Lazy<IDeserializer> _deserializer = new(() => new DeserializerBuilder()
                .WithNamingConvention(new YamlNamingConvention())
                .IgnoreUnmatchedProperties()
                .Build());
    private static IndexRoot deserializeIndexRootFromJson(Stream document) => JsonSerializer.Deserialize<IndexRoot>(document, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
    });
    private static IndexRoot deserializeIndexRootFromYaml(Stream document)
    {
        using var reader = new StreamReader(document);
        return _deserializer.Value.Deserialize<IndexRoot>(reader);
    }
    private async Task<IEnumerable<Tuple<string, SearchResult>>> GetSearchResultsFromRepo(GitHubClient.GitHubClient gitHubClient, RepoSearchResultItem repo, string fileName, string accept, CancellationToken cancellationToken)
    {
        try
        {
            var response = await gitHubClient.Repos[repo.Owner.Login][repo.Name].Contents[fileName].GetAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!response.AdditionalData.TryGetValue("download_url", out var rawDownloadUrl) || rawDownloadUrl is not string downloadUrl || string.IsNullOrEmpty(downloadUrl))
                return Enumerable.Empty<Tuple<string, SearchResult>>();
            await using var document = await cachingProvider.GetDocumentAsync(new Uri(downloadUrl), "search", Path.GetFileName(downloadUrl), accept, cancellationToken);
            var indexFile = accept.ToLowerInvariant() switch
            {
                "application/json" => deserializeIndexRootFromJson(document),
                "text/yaml" => deserializeIndexRootFromYaml(document),
                _ => throw new InvalidOperationException($"Unsupported accept type {accept}"),
            };
            if (indexFile is null || indexFile.Apis is null)
                return Enumerable.Empty<Tuple<string, SearchResult>>();
            return indexFile.Apis.Where(static x => x.Properties.Any(static y => y.Type.Equals(OpenApiPropertyKey, StringComparison.OrdinalIgnoreCase)))
                                .Select(x =>
                                {
                                    var baseUrl = new Uri(x.BaseUrl);
                                    var hostAndPath = baseUrl.Host + baseUrl.AbsolutePath;
                                    return new Tuple<string, SearchResult>($"{repo.Owner.Login}/{repo.Name}/{hostAndPath}",
                                        new SearchResult(x.Name,
                                            x.Description,
                                            new Uri(x.BaseUrl),
                                            new Uri(x.Properties.FirstOrDefault(y => OpenApiPropertyKey.Equals(y.Type, StringComparison.OrdinalIgnoreCase))?.Url),//TODO build the URL if it's relative
                                            new()));
                                });
        }
        catch (BasicError)
        {
            _logger.LogInformation("Unable to find {fileName} in {repoUrl}", fileName, repo.Url);
        }
        catch(Exception ex) when (ex is YamlException || ex is JsonException) {
            #if DEBUG
            _logger.LogError(ex, "Error while parsing the file {fileName} in {repoUrl}", fileName, repo.Url);
            #else
            _logger.LogInformation("Error while parsing the file {fileName} in {repoUrl}", fileName, repo.Url);
            #endif
        }
        return Enumerable.Empty<Tuple<string, SearchResult>>();
    }
    private static async Task<List<RepoSearchResultItem>> GetAllReposForTerm(GitHubClient.GitHubClient gitHubClient, string term, string topic, CancellationToken cancellationToken)
    {
        var results = new List<RepoSearchResultItem>();
        var shouldContinue = false;
        var pageNumber = 1;
        do
        {
            var reposPage = await gitHubClient.Search.Repositories.GetAsync(x =>
            {
                x.QueryParameters.Q = $"{term} topic:{topic}";
                x.QueryParameters.Page = pageNumber;
            }, cancellationToken).ConfigureAwait(false);
            results.AddRange(reposPage.Items);
            shouldContinue = results.Count < reposPage.Total_count;
            pageNumber++;
        } while (shouldContinue);
        return results;
    }
}
