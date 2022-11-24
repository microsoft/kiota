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
                    var sessionStorage = sp.GetRequiredService<ISessionStorageService>();
                    await sessionStorage.SetItemAsync(GitHubStateKey, state, c).ConfigureAwait(false);
                    navManager.NavigateTo(uri.ToString());
                },
                async (c) => {
                    var sessionStorage = sp.GetRequiredService<ISessionStorageService>();
                    var stateValue = await sessionStorage.GetItemAsync<string>(GitHubStateKey).ConfigureAwait(false);
                    var uri = navManager.ToAbsoluteUri(navManager.Uri);
                    var queryStrings = QueryHelpers.ParseQuery(uri.Query);
                    if(queryStrings.TryGetValue("state", out var state) &&
                        stateValue.Equals(state, StringComparison.OrdinalIgnoreCase) &&
                        queryStrings.TryGetValue("code", out var code) && code.FirstOrDefault() is string codeValue)
                            return codeValue;
                    await sessionStorage.RemoveItemAsync(GitHubStateKey, c).ConfigureAwait(false);
                    return string.Empty;
                }, 
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<BrowserAuthenticationProvider>(),
                new Uri($"{baseAddress}GitHubAuth")
            );
        });
    }
    public static void AddPatAuthentication(this IServiceCollection services) {
        services.AddScoped(sp => {
            var configObject = sp.GetRequiredService<KiotaConfiguration>();
            return new TempFolderCachingAccessTokenProvider {
                Logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<TempFolderCachingAccessTokenProvider>()!,
                ApiBaseUrl = configObject.Search.GitHub.ApiBaseUrl,
                Concrete = null,
                AppId = configObject.Search.GitHub.AppId,
            };
        });
        services.AddBlazoredSessionStorage();
        services.AddScoped(sp => {
            var sessionStorage = sp.GetRequiredService<ISessionStorageService>();
            return new SessionStoragePatService {
                SessionStorageService = sessionStorage,
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
                    var patService = sp.GetRequiredService<SessionStoragePatService>();
                    var patValue = await patService.GetPatAsync(c).ConfigureAwait(false);
                    if(!string.IsNullOrEmpty(patValue))
                        return patValue;
                    return string.Empty;
                }
            );
        });
    }
}
