using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Kiota.Builder.SearchProviders.GitHub.Authentication;

public class AnonymousAuthenticationProvider : IAuthenticationProvider
{
    public virtual Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Headers.Add("User-Agent", $"Kiota/{GetType().Assembly.GetName().Version}");
        // request.Headers.Add("X-GitHub-Api-Version", "2022-11-28"); does not support cors today
        return Task.CompletedTask;
    }
}
