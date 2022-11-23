using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Kiota.Builder.SearchProviders.GitHub.Authentication.Browser;
public class AccessTokenProvider : IAccessTokenProvider
{
    public AllowedHostsValidator AllowedHostsValidator {get; set;} = new();
	public required string ClientId { get; init; }
	public required string Scope { get; init; }
    public required Uri RedirectUri { get; init; }
	public required HttpClient HttpClient {get; init;}
    public required Func<Uri, string, CancellationToken, Task> RedirectCallback { get; init; }
    public required Func<CancellationToken, Task<string>> GetAccessCodeCallback { get; init; }
    internal string BaseLoginUrl { get; init; } = "https://github.com/login";
    public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object> additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
    {
        if(!AllowedHostsValidator.IsUrlHostValid(uri))
			return Task.FromResult(string.Empty);
		if(!uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only https is supported");

        return GetAuthorizationTokenInternalAsync(cancellationToken);
    }
	private async Task<string> GetAuthorizationTokenInternalAsync(CancellationToken cancellationToken) {
        var authorizationCode = await GetAccessCodeCallback(cancellationToken);
        if(string.IsNullOrEmpty(authorizationCode)) {
            var state = Guid.NewGuid();
            await RedirectCallback(GetAuthorizeUrl(state), state.ToString(), cancellationToken);
            return string.Empty;
        } else {
            var tokenResponse = await GetTokenAsync(authorizationCode, cancellationToken);
            return tokenResponse.AccessToken;
        }
    }
    private Uri GetAuthorizeUrl(Guid state) {
        var authorizeUrl = $"{BaseLoginUrl}/oauth/authorize?client_id={ClientId}&scope={Scope}&redirect_uri={RedirectUri}&state={state}";
        return new Uri(authorizeUrl);
    }
    private async Task<AccessCodeResponse> GetTokenAsync(string authorizationCode, CancellationToken cancellationToken)
	{
		using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseLoginUrl}/oauth/access_token") {
			Content = new FormUrlEncodedContent(new Dictionary<string, string> {
				{ "client_id", ClientId },
				{ "code", authorizationCode }, //TODO missing secret? what about SPA?
				// { "grant_type", "urn:ietf:params:oauth:grant-type:device_code" }
			})
		};
		tokenRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		using var tokenResponse = await HttpClient.SendAsync(tokenRequest, cancellationToken);
		tokenResponse.EnsureSuccessStatusCode();
		var tokenContent = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
		var result = JsonSerializer.Deserialize<AccessCodeResponse>(tokenContent);
		if ("authorization_pending".Equals(result.Error, StringComparison.OrdinalIgnoreCase))
			return null;
		else if (!string.IsNullOrEmpty(result.Error))
			throw new InvalidOperationException($"Error while getting token: {result.Error} - {result.ErrorDescription}");
		else
			return result;
	}

}
