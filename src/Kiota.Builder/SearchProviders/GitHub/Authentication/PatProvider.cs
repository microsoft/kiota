using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Kiota.Builder.SearchProviders.GitHub.Authentication;

public class PatProvider : IAccessTokenProvider
{
    public AllowedHostsValidator AllowedHostsValidator { get; set; } = new();
    public required Func<CancellationToken, Task<string>> GetPATFromStorageCallback { get; init; }

    public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object> additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
    {
        if("https".Equals(uri.Scheme, StringComparison.OrdinalIgnoreCase) && AllowedHostsValidator.IsUrlHostValid(uri))
            return GetPATFromStorageCallback(cancellationToken);
        return Task.FromResult(string.Empty);
    }
}
