using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.SearchProviders.GitHub;
using Kiota.Builder.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests;

public sealed class KiotaSearcherTests : IDisposable
{
    private readonly HttpClient httpClient = new();
    [Fact]
    public async Task DefensiveProgramingAsync()
    {
        Assert.Throws<ArgumentNullException>(() => new KiotaSearcher(null, new SearchConfiguration(), httpClient, null, null, null));
        Assert.Throws<ArgumentNullException>(() => new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, null, httpClient, null, null, null));
        Assert.Throws<ArgumentNullException>(() => new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, new SearchConfiguration(), null, null, null, null));
        Assert.Throws<ArgumentNullException>(() => new GitHubSearchProvider(httpClient, new Mock<ILogger<KiotaSearcher>>().Object, false, null, null, null));
        Assert.Throws<ArgumentNullException>(() => new GitHubSearchProvider(httpClient, null, false, new GitHubConfiguration(), null, null));
        Assert.Throws<ArgumentNullException>(() => new GitHubSearchProvider(null, new Mock<ILogger<KiotaSearcher>>().Object, false, new GitHubConfiguration(), null, null));
        await Assert.ThrowsAsync<ArgumentNullException>(() => new GitHubSearchProvider(httpClient, new Mock<ILogger<KiotaSearcher>>().Object, false, new GitHubConfiguration(), null, null).SearchAsync(null, null, CancellationToken.None));
    }
    private static SearchConfiguration searchConfigurationFactory => new()
    {
        GitHub = new()
        {
        }
    };
    [RetryFact]
    public async Task GetsMicrosoftGraphBothVersionsAsync()
    {
        var searchConfiguration = searchConfigurationFactory;
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, searchConfiguration, httpClient, null, null, null);
        var results = await searcher.SearchAsync("github::microsoftgraph/msgraph-metadata", string.Empty, new CancellationToken());
        await SkipIfGitHubRateLimitedAsync(results.Count, 2, GitHubCoreRateLimitResource, TestContext.Current.CancellationToken);
        Assert.Equal(2, results.Count);
    }
    [RetryFact]
    public async Task GetsMicrosoftGraphAsync()
    {
        var searchConfiguration = searchConfigurationFactory;
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, searchConfiguration, httpClient, null, null, null);
        var results = await searcher.SearchAsync("github::microsoftgraph/msgraph-metadata/graph.microsoft.com/v1.0", string.Empty, new CancellationToken());
        await SkipIfGitHubRateLimitedAsync(results.Count, 1, GitHubCoreRateLimitResource, TestContext.Current.CancellationToken);
        Assert.Single(results);
        Assert.Equal("https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml", results.First().Value.DescriptionUrl.ToString());
    }
    [RetryFact]
    public async Task GetsMicrosoftGraphBetaAsync()
    {
        var searchConfiguration = searchConfigurationFactory;
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, searchConfiguration, httpClient, null, null, null);
        var results = await searcher.SearchAsync("github::microsoftgraph/msgraph-metadata/graph.microsoft.com/beta", string.Empty, new CancellationToken());
        await SkipIfGitHubRateLimitedAsync(results.Count, 1, GitHubCoreRateLimitResource, TestContext.Current.CancellationToken);
        Assert.Single(results);
        Assert.Equal("https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/beta/openapi.yaml", results.First().Value.DescriptionUrl.ToString());
    }
    [Fact]
    public async Task DoesntFailOnEmptyTermAsync()
    {
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, searchConfigurationFactory, httpClient, null, null, null);
        var results = await searcher.SearchAsync(string.Empty, string.Empty, new CancellationToken());
        Assert.Empty(results);
    }
    [Fact]
    public async Task GetsGithubFromApisGuruAsync()
    {
        var searchConfiguration = searchConfigurationFactory;
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, searchConfiguration, httpClient, null, null, null);
        var results = await searcher.SearchAsync("github", string.Empty, new CancellationToken());
        Assert.NotEmpty(results);
    }
    [Fact]
    public async Task GetsGithubFromApisGuruWithExactMatchAsync()
    {
        var searchConfiguration = searchConfigurationFactory;
        var searcher = new KiotaSearcher(new Mock<ILogger<KiotaSearcher>>().Object, searchConfiguration, httpClient, null, null, null);
        var results = await searcher.SearchAsync("apisguru::github.com:api.github.com.2022-11-28", string.Empty, new CancellationToken());
        Assert.Single(results);
        var result = results.First();
        var resultUrl = result.Value.DescriptionUrl;
        var bytes = await httpClient.GetByteArrayAsync(resultUrl, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotEmpty(bytes);
    }
    private const string GitHubCoreRateLimitResource = "core";

    /// <summary>
    /// The <c>github::</c> searcher relies on the anonymous GitHub REST API, which is limited to
    /// 60 requests/hour per source IP. On shared or network-isolated CI agents that budget is often
    /// already exhausted, in which case <see cref="GitHubSearchProvider"/> swallows the HTTP 403 and
    /// returns no results. When the live query comes back short, probe the (quota-free) rate-limit
    /// endpoint: skip the assertion only when GitHub is genuinely throttled or unreachable, otherwise
    /// let the assertion run so a real product regression still fails the test.
    /// </summary>
    private async Task SkipIfGitHubRateLimitedAsync(int actualCount, int expectedCount, string resource, CancellationToken cancellationToken)
    {
        if (actualCount >= expectedCount)
            return;

        var unavailableReason = await GetGitHubUnavailableReasonAsync(resource, cancellationToken).ConfigureAwait(false);
        if (unavailableReason.Length > 0)
            Assert.Skip(unavailableReason);
    }

    private async Task<string> GetGitHubUnavailableReasonAsync(string resource, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/rate_limit");
            request.Headers.UserAgent.ParseAdd("kiota-tests");
            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var status = (int)response.StatusCode;
            if (status is 403 or 429)
                return $"GitHub API returned HTTP {status} (anonymous rate limit) on this agent; skipping live-network assertion.";
            if (!response.IsSuccessStatusCode)
                return $"GitHub rate-limit probe returned HTTP {status}; GitHub API is unavailable on this agent, skipping live-network assertion.";

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, default, cancellationToken).ConfigureAwait(false);
            if (document.RootElement.TryGetProperty("resources", out var resources)
                && resources.TryGetProperty(resource, out var resourceElement)
                && resourceElement.TryGetProperty("remaining", out var remaining)
                && remaining.TryGetInt32(out var remainingCount)
                && remainingCount == 0)
                return $"GitHub anonymous '{resource}' rate limit is exhausted on this agent; skipping live-network assertion.";

            return string.Empty;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return $"GitHub API is unreachable on this agent ({ex.GetType().Name}); skipping live-network assertion.";
        }
    }
    public void Dispose()
    {
        httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
