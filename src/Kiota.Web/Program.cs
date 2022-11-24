using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Kiota.Web;
using System.Globalization;
using Microsoft.JSInterop;
using Microsoft.Fast.Components.FluentUI;
using BlazorApplicationInsights;
using Kiota.Builder.Configuration;
using Microsoft.AspNetCore.Components;
using Blazored.SessionStorage;
using Microsoft.AspNetCore.WebUtilities;
using Kiota.Web.Authentication.GitHub;
using Kiota.Builder.SearchProviders.GitHub.Authentication;
using Microsoft.Kiota.Abstractions.Authentication;

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
builder.Services.AddSingleton(configObject);
builder.Services.AddBlazoredSessionStorage();
builder.Services.AddScoped(sp => {
    return new TempFolderCachingAccessTokenProvider {
            Logger = sp.GetService<ILoggerFactory>()?.CreateLogger<TempFolderCachingAccessTokenProvider>()!,
            ApiBaseUrl = configObject.Search.GitHub.ApiBaseUrl,
            Concrete = null,
            AppId = configObject.Search.GitHub.AppId,
    };
});
builder.Services.AddScoped<IAuthenticationProvider>(sp => { // TODO move to extension method
    var navManager = sp.GetService<NavigationManager>();
    return new BrowserAuthenticationProvider(
        configObject.Search.GitHub.AppId,
        "repo",
        new string[] { configObject.Search.GitHub.ApiBaseUrl.Host },
        sp.GetService<HttpClient>()!,
        async (uri, state, c) => {
            navManager?.NavigateTo(uri.ToString());
            var sessionStorage = sp.GetService<ISessionStorageService>();
            if(sessionStorage != null)
                await sessionStorage.SetItemAsync(gitHubStateKey, state, c).ConfigureAwait(false);
        },
        async (c) => {
            var sessionStorage = sp.GetService<ISessionStorageService>();
            if(sessionStorage != null) {
                var stateValue = await sessionStorage.GetItemAsync<string>(gitHubStateKey).ConfigureAwait(false);
                if(navManager != null) {
                    var uri = navManager.ToAbsoluteUri(navManager.Uri);
                    var queryStrings = QueryHelpers.ParseQuery(uri.Query);
                    if(queryStrings.TryGetValue("state", out var state) &&
                        stateValue.Equals(state, StringComparison.OrdinalIgnoreCase) &&
                        queryStrings.TryGetValue("code", out var code) && code.FirstOrDefault() is string codeValue)
                            return codeValue;
                }
                await sessionStorage.RemoveItemAsync(gitHubStateKey, c).ConfigureAwait(false);
            }
            return string.Empty;
        }, 
        sp.GetService<ILoggerFactory>()?.CreateLogger<BrowserAuthenticationProvider>()!,
        new Uri($"{builder.HostEnvironment.BaseAddress}/auth")
    );
});

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
