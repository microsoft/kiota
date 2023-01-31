using Deque.AxeCore.Playwright;

namespace Kiota.Web.Tests;

[Collection(PlaywrightFixture.PlaywrightCollection)]
public class KiotaWebGeneralTests : IAsyncDisposable
{
    private readonly PlaywrightFixture playwrightFixture;
    public KiotaWebGeneralTests(PlaywrightFixture playwrightFixture)
    {
        this.playwrightFixture = playwrightFixture;
    }
    public async ValueTask DisposeAsync()
    {
        await playwrightFixture.DisposeAsync();
        GC.SuppressFinalize(this);
    }
    [Fact]
    public async Task PassesAxeAccessibilityReview()
    {
        // Open a page and run test logic.
        await playwrightFixture.GotoPageAsync(
          playwrightFixture.DotnetUrl!,
          async (page) =>
          {
              var axeResults = await page.RunAxe();
              Assert.Empty(axeResults.Violations);
          },
          Browser.Chromium);
    }
}
