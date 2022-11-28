
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Kiota.Builder.SearchProviders.GitHub.Authentication;
public class PatAuthenticationProvider : BaseAuthenticationProvider<PatProvider>
{
    public PatAuthenticationProvider(string clientId, string scope, IEnumerable<string> validHosts, ILogger logger, Func<CancellationToken, Task<string>> GetPATFromStorageCallback) :
        base(clientId, scope, validHosts, logger, (clientId, scope, validHosts) => new PatProvider {
            GetPATFromStorageCallback = GetPATFromStorageCallback,
            AllowedHostsValidator = new (validHosts),
        }, false)
    {
        ArgumentNullException.ThrowIfNull(GetPATFromStorageCallback);
    }
}
