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
public class KiotaSearcherTests {
    [Fact]
    public void DefensivePrograming() {
        Assert.Throws<ArgumentNullException>(() => new KiotaSearcher(null, null));
        Assert.Throws<ArgumentNullException>(() => new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, null));
        Assert.Throws<ArgumentNullException>(() => new KiotaSearcher(null, new SearchConfiguration()));
        Assert.ThrowsAsync<ArgumentNullException>(() => new GitHubSearchProvider(new HttpClient(), new Uri("https://httpbin.org/headers"), new Mock<ILogger<KiotaSearcher>>().Object, false).SearchAsync(null, null, CancellationToken.None));
    }
    [Fact]
    public async Task GetsMicrosoftGraphBothVersions() {
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, new SearchConfiguration(){
            SearchTerm = "github::microsoftgraph/msgraph-metadata",
        });
        var results = await searcher.SearchAsync(new CancellationToken());
        Assert.Equal(2, results.Count);
    }
    [Fact]
    public async Task GetsMicrosoftGraph() {
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, new SearchConfiguration(){
            SearchTerm = "github::microsoftgraph/msgraph-metadata/graph.microsoft.com/v1.0",
        });
        var results = await searcher.SearchAsync(new CancellationToken());
        Assert.Single(results);
        Assert.Equal("https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml", results.First().Value.DescriptionUrl.ToString());
    }
    [Fact]
    public async Task GetsMicrosoftGraphBeta() {
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, new SearchConfiguration(){
            SearchTerm = "github::microsoftgraph/msgraph-metadata/graph.microsoft.com/beta",
        });
        var results = await searcher.SearchAsync(new CancellationToken());
        Assert.Single(results);
        Assert.Equal("https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/beta/openapi.yaml", results.First().Value.DescriptionUrl.ToString());
    }
    [Fact]
    public async Task DoesntFailOnEmptyTerm() {
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, new SearchConfiguration());
        var results = await searcher.SearchAsync(new CancellationToken());
        Assert.Empty(results);
    }
    [Fact]
    public async Task GetsGithubFromApisGuru() {
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, new SearchConfiguration(){
            SearchTerm = "github",
        });
        var results = await searcher.SearchAsync(new CancellationToken());
        Assert.NotEmpty(results);
    }
    [Fact]
    public async Task GetsGithubFromApisGuruWithExactMatch() {
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, new SearchConfiguration(){
            SearchTerm = "apisguru::github.com:api.github.com",
        });
        var results = await searcher.SearchAsync(new CancellationToken());
        Assert.Single(results);
    }
}
