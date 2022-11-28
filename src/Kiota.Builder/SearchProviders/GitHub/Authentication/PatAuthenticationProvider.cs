
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Kiota.Builder.SearchProviders.GitHub.Authentication;
public class PatAuthenticationProvider : BaseAuthenticationProvider<PatProvider>
{
    public PatAuthenticationProvider(string clientId, string scope, IEnumerable<string> validHosts, ILogger logger, ITokenStorageService StorageService) :
        base(clientId, scope, validHosts, logger, (clientId, scope, validHosts) => new PatProvider {
            StorageService = StorageService,
            AllowedHostsValidator = new (validHosts),
        }, false)
    {
        ArgumentNullException.ThrowIfNull(StorageService);
    }
}
