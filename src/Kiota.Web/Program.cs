using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Kiota.Web;
using System.Globalization;
using Microsoft.JSInterop;
using Microsoft.Fast.Components.FluentUI;
using BlazorApplicationInsights;
using Microsoft.Kiota.Abstractions.Authentication;
using Kiota.Builder.SearchProviders.GitHub.Authentication.Browser;
using Kiota.Builder.Configuration;
using Microsoft.AspNetCore.Components;
using Blazored.SessionStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

var gitHubStateKey = "github-authentication-state";
builder.Services.AddLocalization();
builder.Services.AddFluentUIComponents();
builder.Services.AddBlazorApplicationInsights();
var configObject = new KiotaConfiguration();
builder.Configuration.Bind(configObject);
builder.Services.AddBlazoredSessionStorage();
builder.Services.AddScoped<IAuthenticationProvider>(sp => new BrowserAuthenticationProvider(
    configObject.Search.GitHub.AppId,
    "repo",
    new string[] { configObject.Search.GitHub.ApiBaseUrl.Host },
    sp.GetService<HttpClient>(),
    async (uri, state, c) => {
        sp.GetService<NavigationManager>()?.NavigateTo(uri.ToString());
        var sessionStorage = sp.GetService<ISessionStorageService>();
        if(sessionStorage != null)
            await sessionStorage.SetItemAsync(gitHubStateKey, state, c).ConfigureAwait(false);
    },
    async (c) => {
        var sessionStorage = sp.GetService<ISessionStorageService>();
        if(sessionStorage != null) {
            var stateValue = await sessionStorage.GetItemAsync<string>(gitHubStateKey).ConfigureAwait(false);
            //TODO compare state value
            //TODO get authorization code from query string
            await sessionStorage.RemoveItemAsync(gitHubStateKey, c).ConfigureAwait(false);
        }
        return string.Empty;
    }, 
    sp.GetService<ILoggerFactory>()?.CreateLogger<BrowserAuthenticationProvider>(),
    new Uri($"{builder.HostEnvironment.BaseAddress}/auth")
));

var host = builder.Build();

CultureInfo culture;
var js = host.Services.GetRequiredService<IJSRuntime>();
var result = await js.InvokeAsync<string>("blazorCulture.get");

if (result != null)
{
    culture = new CultureInfo(result);
}
else
{
    culture = new CultureInfo("en-US");
    await js.InvokeVoidAsync("blazorCulture.set", "en-US");
}

CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

await host.RunAsync();
