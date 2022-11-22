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
        Assert.ThrowsAsync<ArgumentNullException>(() => new GitHubSearchProvider(httpClient, new Uri("https://httpbin.org/headers"), new Mock<ILogger<KiotaSearcher>>().Object, false).SearchAsync(null, null, CancellationToken.None));
    }
    [Fact]
    public async Task GetsMicrosoftGraphBothVersions() {
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, new SearchConfiguration(){
            SearchTerm = "github::microsoftgraph/msgraph-metadata",
        }, httpClient);
        var results = await searcher.SearchAsync(new CancellationToken());
        Assert.Equal(2, results.Count);
    }
    [Fact]
    public async Task GetsMicrosoftGraph() {
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, new SearchConfiguration(){
            SearchTerm = "github::microsoftgraph/msgraph-metadata/graph.microsoft.com/v1.0",
        }, httpClient);
        var results = await searcher.SearchAsync(new CancellationToken());
        Assert.Single(results);
        Assert.Equal("https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml", results.First().Value.DescriptionUrl.ToString());
    }
    [Fact]
    public async Task GetsMicrosoftGraphBeta() {
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, new SearchConfiguration(){
            SearchTerm = "github::microsoftgraph/msgraph-metadata/graph.microsoft.com/beta",
        }, httpClient);
        var results = await searcher.SearchAsync(new CancellationToken());
        Assert.Single(results);
        Assert.Equal("https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/beta/openapi.yaml", results.First().Value.DescriptionUrl.ToString());
    }
    [Fact]
    public async Task DoesntFailOnEmptyTerm() {
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, new SearchConfiguration(), httpClient);
        var results = await searcher.SearchAsync(new CancellationToken());
        Assert.Empty(results);
    }
    [Fact]
    public async Task GetsGithubFromApisGuru() {
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, new SearchConfiguration(){
            SearchTerm = "github",
        }, httpClient);
        var results = await searcher.SearchAsync(new CancellationToken());
        Assert.NotEmpty(results);
    }
    [Fact]
    public async Task GetsGithubFromApisGuruWithExactMatch() {
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, new SearchConfiguration(){
            SearchTerm = "apisguru::github.com:api.github.com",
        }, httpClient);
        var results = await searcher.SearchAsync(new CancellationToken());
        Assert.Single(results);
    }
    public void Dispose()
    {
        httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
