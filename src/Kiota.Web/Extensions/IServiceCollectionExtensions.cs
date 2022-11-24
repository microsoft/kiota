using Blazored.SessionStorage;
using Kiota.Builder.Configuration;
using Kiota.Builder.SearchProviders.GitHub.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Kiota.Web.Authentication.GitHub;

public static class IServiceCollectionExtensions {
    private const string GitHubStateKey = "github-authentication-state";
    public static void AddBrowserCodeAuthentication(this IServiceCollection services, string baseAddress) {
        services.AddBlazoredSessionStorage();
        services.AddScoped(sp => {
            var configObject = sp.GetRequiredService<KiotaConfiguration>();
            return new TempFolderCachingAccessTokenProvider {
                    Logger = sp.GetService<ILoggerFactory>()?.CreateLogger<TempFolderCachingAccessTokenProvider>()!,
                    ApiBaseUrl = configObject.Search.GitHub.ApiBaseUrl,
                    Concrete = null,
                    AppId = configObject.Search.GitHub.AppId,
            };
        });
        services.AddScoped<IAuthenticationProvider>(sp => {
            var configObject = sp.GetRequiredService<KiotaConfiguration>();
            var navManager = sp.GetRequiredService<NavigationManager>();
            return new BrowserAuthenticationProvider(
                configObject.Search.GitHub.AppId,
                "repo",
                new string[] { configObject.Search.GitHub.ApiBaseUrl.Host },
                sp.GetService<HttpClient>()!,
                async (uri, state, c) => {
                    navManager.NavigateTo(uri.ToString());
                    var sessionStorage = sp.GetService<ISessionStorageService>();
                    if(sessionStorage != null)
                        await sessionStorage.SetItemAsync(GitHubStateKey, state, c).ConfigureAwait(false);
                },
                async (c) => {
                    var sessionStorage = sp.GetService<ISessionStorageService>();
                    if(sessionStorage != null) {
                        var stateValue = await sessionStorage.GetItemAsync<string>(GitHubStateKey).ConfigureAwait(false);
                        if(navManager != null) {
                            var uri = navManager.ToAbsoluteUri(navManager.Uri);
                            var queryStrings = QueryHelpers.ParseQuery(uri.Query);
                            if(queryStrings.TryGetValue("state", out var state) &&
                                stateValue.Equals(state, StringComparison.OrdinalIgnoreCase) &&
                                queryStrings.TryGetValue("code", out var code) && code.FirstOrDefault() is string codeValue)
                                    return codeValue;
                        }
                        await sessionStorage.RemoveItemAsync(GitHubStateKey, c).ConfigureAwait(false);
                    }
                    return string.Empty;
                }, 
                sp.GetService<ILoggerFactory>()?.CreateLogger<BrowserAuthenticationProvider>()!,
                new Uri($"{baseAddress}GitHubAuth")
            );
        });
    }
}
