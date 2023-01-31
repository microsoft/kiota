using System.Diagnostics;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Playwright;

namespace Kiota.Web.Tests;
/// <summary>
/// Playwright fixture implementing an asynchronous life cycle.
/// </summary>
public class PlaywrightFixture : IAsyncLifetime
{
    /// <summary>
    /// Playwright module.
    /// </summary>
    public IPlaywright? Playwright
    {
        get; private set;
    }
    /// <summary>
    /// Chromium lazy initializer.
    /// </summary>
    public IBrowser? ChromiumBrowser
    {
        get; private set;
    }

    /// <summary>
    /// Firefox lazy initializer.
    /// </summary>
    public IBrowser? FirefoxBrowser
    {
        get; private set;
    }

    /// <summary>
    /// Webkit lazy initializer.
    /// </summary>
    public IBrowser? WebkitBrowser
    {
        get; private set;
    }
    /// <summary>
    /// The process running the blazor app.
    /// </summary>
    public Process? DotnetRunProcess
    {
        get; private set;
    }
    /// <summary>
    /// The URL the app is hosted at
    /// </summary>
    public string? DotnetUrl
    {
        get; private set;
    }
    private static readonly Regex urlRegex = new Regex(@"Now listening on: (?<url>.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    /// <summary>
    /// Initialize the Playwright fixture.
    /// </summary>
    public async Task InitializeAsync()
    {
        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = true,
        };

        // Install Playwright and its dependencies.
        InstallPlaywright();
        // Create Playwright module.
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        // Setup Browser lazy initializers.
        ChromiumBrowser = await Playwright.Chromium.LaunchAsync(launchOptions);
        FirefoxBrowser = await Playwright.Firefox.LaunchAsync(launchOptions);
        WebkitBrowser = await Playwright.Webkit.LaunchAsync(launchOptions);

        // Start the blazor app.
        DotnetRunProcess = new();
        DotnetRunProcess.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
        {
            // Prepend line numbers to each line of the output.
            if (!string.IsNullOrEmpty(e.Data) && urlRegex.IsMatch(e.Data))
            {
                var match = urlRegex.Match(e.Data);
                DotnetUrl = match.Groups["url"].Value;
            }
        });
        DotnetRunProcess.StartInfo = new()
        {
            FileName = "dotnet",
            Arguments = "run",
            WorkingDirectory = Path.GetFullPath("../../../../../src/Kiota.Web"),
            RedirectStandardOutput = true,
        };
        DotnetRunProcess.Start();
        DotnetRunProcess.BeginOutputReadLine();
        var secondsToWaitForRunToStart = 300;
        while (DotnetUrl == null && secondsToWaitForRunToStart > 0)
        {
            secondsToWaitForRunToStart--;
            await Task.Delay(1000);
        }
        if (DotnetUrl == null)
        {
            DotnetRunProcess.Kill(true);
            throw new Exception("Failed to start the blazor app.");
        }
        DotnetRunProcess.CancelOutputRead();
    }
    /// <summary>
    /// Dispose all Playwright module resources.
    /// </summary>
    public async Task DisposeAsync()
    {
        await (ChromiumBrowser?.CloseAsync() ?? Task.CompletedTask);
        await (FirefoxBrowser?.CloseAsync() ?? Task.CompletedTask);
        await (WebkitBrowser?.CloseAsync() ?? Task.CompletedTask);
        Playwright?.Dispose();
        Playwright = null;
        DotnetRunProcess?.Kill(true);
        DotnetRunProcess?.Dispose();
    }

    /// <summary>
    /// Install and deploy all binaries Playwright may need.
    /// </summary>
    private static void InstallPlaywright()
    {
        var exitCode = Microsoft.Playwright.Program.Main(
          new[] { "install-deps" });
        if (exitCode != 0)
        {
            throw new Exception(
              $"Playwright exited with code {exitCode} on install-deps");
        }
        exitCode = Microsoft.Playwright.Program.Main(new[] { "install" });
        if (exitCode != 0)
        {
            throw new Exception(
              $"Playwright exited with code {exitCode} on install");
        }
    }

    /// <summary>
    /// PlaywrightCollection name that is used in the Collection
    /// attribute on each test classes.
    /// Like "[Collection(PlaywrightFixture.PlaywrightCollection)]"
    /// </summary>
    public const string PlaywrightCollection =
      nameof(PlaywrightCollection);
    [CollectionDefinition(PlaywrightCollection)]
    public class PlaywrightCollectionDefinition
      : ICollectionFixture<PlaywrightFixture>
    {
        // This class is just xUnit plumbing code to apply
        // [CollectionDefinition] and the ICollectionFixture<>
        // interfaces. Witch in our case is parametrized
        // with the PlaywrightFixture.
    }

    /// <summary>
    /// Open a Browser page and navigate to the given URL before
    /// applying the given test handler.
    /// </summary>
    /// <param name="url">URL to navigate to.</param>
    /// <param name="testHandler">Test handler to apply on the page.
    /// </param>
    /// <param name="browserType">The Browser to use to open the page.
    /// </param>
    /// <returns>The GotoPage task.</returns>
    public async Task GotoPageAsync(
        string url,
        Func<IPage, Task> testHandler,
        Browser browserType)
    {
        // select and launch the browser.
        var browser = SelectBrowser(browserType);

        // Open a new page with an option to ignore HTTPS errors
        await using var context = await browser.NewContextAsync(
            new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = true
            }).ConfigureAwait(false);

        // Start tracing before creating the page.
        await context.Tracing.StartAsync(new TracingStartOptions()
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });

        var page = await context.NewPageAsync().ConfigureAwait(false);

        page.Should().NotBeNull();
        try
        {
            // Navigate to the given URL and wait until loading
            // network activity is done.
            var gotoResult = await page.GotoAsync(
              url,
              new PageGotoOptions
              {
                  WaitUntil = WaitUntilState.NetworkIdle
              });
            if (gotoResult == null)
            {
                throw new Exception(
                  $"Failed to navigate to {url}.");
            }
            gotoResult.Should().NotBeNull();
            await gotoResult.FinishedAsync();
            gotoResult.Ok.Should().BeTrue();
            // Run the actual test logic.
            await testHandler(page);
        }
        finally
        {
            // Make sure the page is closed 
            await page.CloseAsync();

            // Stop tracing and save data into a zip archive.
            await context.Tracing.StopAsync(new TracingStopOptions()
            {
                Path = "trace.zip"
            });
        }
    }
    /// <summary>
    /// Select the IBrowser instance depending on the given browser
    /// enumeration value.
    /// </summary>
    /// <param name="browser">The browser to select.</param>
    /// <returns>The selected IBrowser instance.</returns>
    private IBrowser SelectBrowser(Browser browser)
    {
        return browser switch
        {
            Browser.Chromium => ChromiumBrowser ?? throw new Exception(
              "Chromium browser is not initialized"),
            Browser.Firefox => FirefoxBrowser ?? throw new Exception(
              "Firefox browser is not initialized"),
            Browser.Webkit => WebkitBrowser ?? throw new Exception(
              "Webkit browser is not initialized"),
            _ => throw new NotImplementedException(),
        };
    }
}

/// <summary>
/// Browser types we can use in the PlaywrightFixture.
/// </summary>
public enum Browser
{
    Chromium,
    Firefox,
    Webkit,
}
