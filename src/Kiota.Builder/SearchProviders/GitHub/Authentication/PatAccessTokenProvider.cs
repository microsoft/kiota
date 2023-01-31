﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Kiota.Builder.SearchProviders.GitHub.Authentication;

public class PatAccessTokenProvider : IAccessTokenProvider
{
    public AllowedHostsValidator AllowedHostsValidator { get; set; } = new();
    public required ITokenStorageService StorageService
    {
        get; init;
    }
    public async Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if ("https".Equals(uri.Scheme, StringComparison.OrdinalIgnoreCase) && AllowedHostsValidator.IsUrlHostValid(uri))
            return await StorageService.GetTokenAsync(cancellationToken) ?? string.Empty;
        return string.Empty;
    }
}
