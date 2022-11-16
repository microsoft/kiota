namespace Kiota.Web.Tests;

[Collection(PlaywrightFixture.PlaywrightCollection)]
public class UnitTest1
{
    private readonly PlaywrightFixture playwrightFixture;
    public UnitTest1(PlaywrightFixture playwrightFixture)
    {
        this.playwrightFixture = playwrightFixture;
    }
    [Fact]
    public async Task MyFirstTest()
    {
        // Open a page and run test logic.
        await playwrightFixture.GotoPageAsync(
          playwrightFixture.DotnetUrl!,
          async (page) =>
          {
              await page.Locator("text=Search").ClickAsync();
          },
          Browser.Chromium);
        await playwrightFixture.DisposeAsync(); //TODO run that at the end of the test suite
    }
}
