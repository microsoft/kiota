using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
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
    }
    [Fact]
    public async Task GetsMicrosoftGraph() {
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, new SearchConfiguration(){
            SearchTerm = "msgraph::microsoft-graph",
        });
        var results = await searcher.SearchAsync(new CancellationToken());
        Assert.Single(results);
    }
    [Fact]
    public async Task GetsMicrosoftGraphBeta() {
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, new SearchConfiguration(){
            SearchTerm = "msgraph::microsoft-graph",
            Version = "beta",
        });
        var results = await searcher.SearchAsync(new CancellationToken());
        Assert.Equal("https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/beta/openapi.yaml", results.First().Value.DescriptionUrl.ToString());
    }
    [Fact]
    public async Task DoesntFailOnEmptyTerm() {
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, new SearchConfiguration());
        var results = await searcher.SearchAsync(new CancellationToken());
        Assert.Empty(results);
    }
}
