using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Extensions.Logging;

namespace Kiota.Builder.SearchProviders.GitHub.Authentication;

public class TempFolderCachingAccessTokenProvider : IAccessTokenProvider
{
    public required IAccessTokenProvider? Concrete { get; init; }
    public required ILogger Logger { get; init; }
    public required Uri ApiBaseUrl { get; init; }
    public required string AppId { get; init; }
    public AllowedHostsValidator AllowedHostsValidator => Concrete?.AllowedHostsValidator ?? new();
    public readonly Lazy<ITokenStorageService> TokenStorageService;
    public TempFolderCachingAccessTokenProvider()
    {
        TokenStorageService = new Lazy<ITokenStorageService>(() => new TempFolderTokenStorageService {
            Logger = Logger!,
            FileName = $"{AppId}-{ApiBaseUrl?.Host}",
        });
    }
    public async Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
    {
        var result = await TokenStorageService.Value.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(result) && Concrete is not null)
        {
            Logger.LogInformation("Token not found in cache, requesting a new one");
            result = await Concrete.GetAuthorizationTokenAsync(uri, additionalAuthenticationContext, cancellationToken).ConfigureAwait(false);
            await TokenStorageService.Value.SetTokenAsync(result, cancellationToken).ConfigureAwait(false);
        }
        return result;
    }
    
}
