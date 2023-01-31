﻿using System.Net.Http.Headers;
using System.Net.Http.Json;
using Kiota.Builder.SearchProviders.GitHub.Authentication;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Kiota.Web.Authentication.GitHub;
public class BrowserAccessTokenProvider : IAccessTokenProvider
{
    public AllowedHostsValidator AllowedHostsValidator { get; set; } = new();
    public required string ClientId
    {
        get; init;
    }
    public required string Scope
    {
        get; init;
    }
    public required Uri RedirectUri
    {
        get; init;
    }
    public required HttpClient HttpClient
    {
        get; init;
    }
    public required Func<Uri, string, CancellationToken, Task> RedirectCallback
    {
        get; init;
    }
    public required Func<CancellationToken, Task<string>> GetAccessCodeCallback
    {
        get; init;
    }
    internal string BaseLoginUrl { get; init; } = "https://github.com/login";
    public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
    {
        if (!AllowedHostsValidator.IsUrlHostValid(uri))
            return Task.FromResult(string.Empty);
        if (!uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only https is supported");

        return GetAuthorizationTokenInternalAsync(cancellationToken);
    }
    private async Task<string> GetAuthorizationTokenInternalAsync(CancellationToken cancellationToken)
    {
        var authorizationCode = await GetAccessCodeCallback(cancellationToken);
        if (string.IsNullOrEmpty(authorizationCode))
        {
            var state = Guid.NewGuid();
            await RedirectCallback(GetAuthorizeUrl(state), state.ToString(), cancellationToken);
            return string.Empty;
        }
        else
        {
            var tokenResponse = await GetTokenAsync(authorizationCode, cancellationToken);
            return tokenResponse?.AccessToken ?? string.Empty;
        }
    }
    private Uri GetAuthorizeUrl(Guid state)
    {
        var authorizeUrl = $"{BaseLoginUrl}/oauth/authorize?client_id={ClientId}&scope={Scope}&redirect_uri={RedirectUri}&state={state}";
        return new Uri(authorizeUrl);
    }
    private async Task<AccessCodeResponse?> GetTokenAsync(string authorizationCode, CancellationToken cancellationToken)
    {
        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseLoginUrl}/oauth/access_token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> {
                { "client_id", ClientId },
                { "code", authorizationCode }, 
                // acquisition doesn't work because the endpoint doesn't support CORS or PKCE, and requires a secret
                // we're leaving the code in place as we hope that GitHub will eventually support this and it'll be a matter of updating the parameters and calling AddBrowserCodeAuthentication
			})
        };
        tokenRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var tokenResponse = await HttpClient.SendAsync(tokenRequest, cancellationToken);
        tokenResponse.EnsureSuccessStatusCode();
        var result = await tokenResponse.Content.ReadFromJsonAsync<AccessCodeResponse>(cancellationToken: cancellationToken);
        if ("authorization_pending".Equals(result?.Error, StringComparison.OrdinalIgnoreCase))
            return null;
        else if (!string.IsNullOrEmpty(result?.Error))
            throw new InvalidOperationException($"Error while getting token: {result.Error} - {result.ErrorDescription}");
        else
            return result;
    }

}
