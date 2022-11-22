using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.SearchProviders.GitHub;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests;
public class KiotaSearcherTests : IDisposable {
    private readonly HttpClient httpClient = new();
    [Fact]
    public void DefensivePrograming() {
        Assert.Throws<ArgumentNullException>(() => new KiotaSearcher(null, new SearchConfiguration(), httpClient));
        Assert.Throws<ArgumentNullException>(() => new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, null, httpClient));
        Assert.Throws<ArgumentNullException>(() => new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, new SearchConfiguration(), null));
        Assert.Throws<ArgumentNullException>(() => new GitHubSearchProvider(httpClient, new Mock<ILogger<KiotaSearcher>>().Object, false, null));
        Assert.Throws<ArgumentNullException>(() => new GitHubSearchProvider(httpClient, null, false, new GitHubConfiguration()));
        Assert.Throws<ArgumentNullException>(() => new GitHubSearchProvider(null, new Mock<ILogger<KiotaSearcher>>().Object, false, new GitHubConfiguration()));
        Assert.ThrowsAsync<ArgumentNullException>(() => new GitHubSearchProvider(httpClient, new Mock<ILogger<KiotaSearcher>>().Object, false, new GitHubConfiguration()).SearchAsync(null, null, CancellationToken.None));
    }
    private static SearchConfiguration searchConfigurationFactory => new(){
        GitHub = new() {
            DeviceCodeCallback = (code, uri) => {},
        }
    };
    [Fact]
    public async Task GetsMicrosoftGraphBothVersions() {
        var searchConfiguration = searchConfigurationFactory;
        searchConfiguration.SearchTerm = "github::microsoftgraph/msgraph-metadata";
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, searchConfiguration, httpClient);
        var results = await searcher.SearchAsync(new CancellationToken());
        Assert.Equal(2, results.Count);
    }
    [Fact]
    public async Task GetsMicrosoftGraph() {
        var searchConfiguration = searchConfigurationFactory;
        searchConfiguration.SearchTerm = "github::microsoftgraph/msgraph-metadata/graph.microsoft.com/v1.0";
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, searchConfiguration, httpClient);
        var results = await searcher.SearchAsync(new CancellationToken());
        Assert.Single(results);
        Assert.Equal("https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml", results.First().Value.DescriptionUrl.ToString());
    }
    [Fact]
    public async Task GetsMicrosoftGraphBeta() {
        var searchConfiguration = searchConfigurationFactory;
        searchConfiguration.SearchTerm = "github::microsoftgraph/msgraph-metadata/graph.microsoft.com/beta";
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, searchConfiguration, httpClient);
        var results = await searcher.SearchAsync(new CancellationToken());
        Assert.Single(results);
        Assert.Equal("https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/beta/openapi.yaml", results.First().Value.DescriptionUrl.ToString());
    }
    [Fact]
    public async Task DoesntFailOnEmptyTerm() {
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, searchConfigurationFactory, httpClient);
        var results = await searcher.SearchAsync(new CancellationToken());
        Assert.Empty(results);
    }
    [Fact]
    public async Task GetsGithubFromApisGuru() {
        var searchConfiguration = searchConfigurationFactory;
        searchConfiguration.SearchTerm = "github";
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, searchConfiguration, httpClient);
        var results = await searcher.SearchAsync(new CancellationToken());
        Assert.NotEmpty(results);
    }
    [Fact]
    public async Task GetsGithubFromApisGuruWithExactMatch() {
        var searchConfiguration = searchConfigurationFactory;
        searchConfiguration.SearchTerm = "apisguru::github.com:api.github.com";
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, searchConfiguration, httpClient);
        var results = await searcher.SearchAsync(new CancellationToken());
        Assert.Single(results);
    }
    public void Dispose()
    {
        httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
