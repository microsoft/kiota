using Blazored.LocalStorage;
using Kiota.Builder;
using Kiota.Builder.Configuration;
using Kiota.Builder.SearchProviders.GitHub.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Kiota.Web.Authentication.GitHub;

public static class IServiceCollectionExtensions {
    private const string GitHubStateKey = "github-authentication-state";
    public static void AddBrowserCodeAuthentication(this IServiceCollection services, string baseAddress) {
        services.AddBlazoredLocalStorage();
        services.AddScoped(sp => {
            var configObject = sp.GetRequiredService<KiotaConfiguration>();
            return new TempFolderCachingAccessTokenProvider {
                Logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<TempFolderCachingAccessTokenProvider>()!,
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
                sp.GetRequiredService<HttpClient>(),
                async (uri, state, c) => {
                    var localStorage = sp.GetRequiredService<ILocalStorageService>();
                    await localStorage.SetItemAsync(GitHubStateKey, state, c).ConfigureAwait(false);
                    navManager.NavigateTo(uri.ToString());
                },
                async (c) => {
                    var localStorage = sp.GetRequiredService<ILocalStorageService>();
                    var stateValue = await localStorage.GetItemAsync<string>(GitHubStateKey, c).ConfigureAwait(false);
                    var uri = navManager.ToAbsoluteUri(navManager.Uri);
                    var queryStrings = QueryHelpers.ParseQuery(uri.Query);
                    if(queryStrings.TryGetValue("state", out var state) &&
                        stateValue.Equals(state, StringComparison.OrdinalIgnoreCase) &&
                        queryStrings.TryGetValue("code", out var code) && code.FirstOrDefault() is string codeValue)
                            return codeValue;
                    await localStorage.RemoveItemAsync(GitHubStateKey, c).ConfigureAwait(false);
                    return string.Empty;
                }, 
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<BrowserAuthenticationProvider>(),
                new Uri($"{baseAddress}GitHubAuth")
            );
        });
    }
    public static void AddPatAuthentication(this IServiceCollection services) {
        services.AddBlazoredLocalStorage();
        services.AddScoped<ITokenStorageService>(sp => {
            var localStorage = sp.GetRequiredService<ILocalStorageService>();
            return new LocalStorageTokenStorageService {
                LocalStorageService = localStorage,
            };
        });
        services.AddScoped<IAuthenticationProvider>(sp => {
            var configObject = sp.GetRequiredService<KiotaConfiguration>();
            return new PatAuthenticationProvider(
                configObject.Search.GitHub.AppId,
                "repo",
                new string[] { configObject.Search.GitHub.ApiBaseUrl.Host },
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<PatAuthenticationProvider>(),
                async (c) => {
                    var patService = sp.GetRequiredService<ITokenStorageService>();
                    var patValue = await patService.GetTokenAsync(c).ConfigureAwait(false);
                    if(!string.IsNullOrEmpty(patValue))
                        return patValue;
                    return string.Empty;
                }
            );
        });
    }
    public static void AddSearchService(this IServiceCollection services) {
        services.AddScoped(sp => {
            var configObject = sp.GetRequiredService<KiotaConfiguration>();
            var patService = sp.GetRequiredService<ITokenStorageService>();
            return new KiotaSearcher(sp.GetRequiredService<ILoggerFactory>().CreateLogger<KiotaSearcher>(),
                                configObject.Search, 
                                sp.GetRequiredService<HttpClient>(),
                                sp.GetRequiredService<IAuthenticationProvider>(),
                                (c) => patService.IsTokenPresentAsync(c));
        });
    }
}
