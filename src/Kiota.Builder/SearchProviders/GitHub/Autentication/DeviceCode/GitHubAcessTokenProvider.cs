using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Kiota.Builder.SearchProviders.GitHub.Authentication.DeviceCode;
internal class GitHubAccessTokenProvider : IAccessTokenProvider
{
    internal required Action<Uri, string> MessageCallback { get; init; }
    public AllowedHostsValidator AllowedHostsValidator {get;set;} = new ();
	internal required string ClientId { get; init; }
	internal required string Scope { get; init; }
	internal required HttpClient HttpClient {get; init;}
    internal string BaseLoginUrl { get; init; } = "https://github.com/login";
	public async Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object> additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
	{
		if(!AllowedHostsValidator.IsUrlHostValid(uri))
			return string.Empty;
		if(!uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only https is supported");

		var deviceCodeResponse = await GetDeviceCodeAsync(cancellationToken);
        MessageCallback(deviceCodeResponse.VerificationUri, deviceCodeResponse.UserCode);
		var tokenResponse = await PollForTokenAsync(deviceCodeResponse, cancellationToken);
		return tokenResponse.AccessToken;
	}
	private async Task<GitHubAccessCodeResponse> PollForTokenAsync(GitHubDeviceCodeResponse deviceCodeResponse, CancellationToken cancellationToken)
	{
		var timeOutTask = Task.Delay(TimeSpan.FromSeconds(deviceCodeResponse.ExpiresInSeconds), cancellationToken);
		var pollTask = Task.Run(async () => {
			while(!cancellationToken.IsCancellationRequested) {
				var tokenResponse = await GetTokenAsync(deviceCodeResponse, cancellationToken);
				if(tokenResponse != null)
					return tokenResponse;
				await Task.Delay(TimeSpan.FromSeconds(deviceCodeResponse.IntervalInSeconds), cancellationToken);
			}
			return null;
		}, cancellationToken);
		var completedTask = await Task.WhenAny(timeOutTask, pollTask);
		if(completedTask == timeOutTask)
			throw new TimeoutException("The device code has expired.");
		return await pollTask;
	}
	private async Task<GitHubAccessCodeResponse> GetTokenAsync(GitHubDeviceCodeResponse deviceCodeResponse, CancellationToken cancellationToken)
	{
		using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseLoginUrl}/oauth/access_token") {
			Content = new FormUrlEncodedContent(new Dictionary<string, string> {
				{ "client_id", ClientId },
				{ "device_code", deviceCodeResponse.DeviceCode },
				{ "grant_type", "urn:ietf:params:oauth:grant-type:device_code" }
			})
		};
		tokenRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		using var tokenResponse = await HttpClient.SendAsync(tokenRequest, cancellationToken);
		tokenResponse.EnsureSuccessStatusCode();
		var tokenContent = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
		var result = JsonSerializer.Deserialize<GitHubAccessCodeResponse>(tokenContent);
		if ("authorization_pending".Equals(result.Error, StringComparison.OrdinalIgnoreCase))
			return null;
		else if (!string.IsNullOrEmpty(result.Error))
			throw new Exception($"Error while getting token: {result.Error} - {result.ErrorDescription}");
		else
			return result;
	}

	private async Task<GitHubDeviceCodeResponse> GetDeviceCodeAsync(CancellationToken cancellationToken) {
		using var codeRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseLoginUrl}/device/code") {
			Content = new FormUrlEncodedContent(new Dictionary<string, string> {
				{ "client_id", ClientId },
				{ "scope", Scope }
			})
		};
		codeRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		using var codeResponse = await HttpClient.SendAsync(codeRequest, cancellationToken);
		codeResponse.EnsureSuccessStatusCode();
		var codeResponseContent = await codeResponse.Content.ReadAsStringAsync(cancellationToken);
		return JsonSerializer.Deserialize<GitHubDeviceCodeResponse>(codeResponseContent);
	}
}
