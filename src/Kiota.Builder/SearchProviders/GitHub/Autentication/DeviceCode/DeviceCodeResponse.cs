using System;
using System.Text.Json.Serialization;

namespace Kiota.Builder.SearchProviders.GitHub.Authentication.DeviceCode;

internal class GitHubDeviceCodeResponse {
	[JsonPropertyName("device_code")]
	public string DeviceCode { get; set; }
	[JsonPropertyName("user_code")]
	public string UserCode { get; set; }
	[JsonPropertyName("verification_uri")]
	public Uri VerificationUri { get; set; }
	[JsonPropertyName("expires_in")]
	public uint ExpiresInSeconds { get; set; }
	[JsonPropertyName("interval")]
	public uint IntervalInSeconds { get; set; }
}
