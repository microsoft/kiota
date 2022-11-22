using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Kiota.Builder.SearchProviders.GitHub.Authentication;
public class GitHubAnonymousAuthenticationProvider : IAuthenticationProvider
{
    public virtual Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object> additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
    {
        request.Headers.Add("User-Agent", $"Kiota/{GetType().Assembly.GetName().Version}");
        return Task.CompletedTask;
    }
}
