
using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Kiota.Builder.SearchProviders.GitHub.Authentication.DeviceCode;

public class DeviceCodeAuthenticationProvider : BaseAuthenticationProvider<AccessTokenProvider>
{
    public DeviceCodeAuthenticationProvider(string clientId, string scope, IEnumerable<string> validHosts, HttpClient httpClient, Action<Uri, string> messageCallback, ILogger logger) :
        base(clientId, scope, validHosts, logger, (clientId, scope, validHosts) => new AccessTokenProvider {
            ClientId = clientId,
            HttpClient = httpClient,
            MessageCallback = messageCallback,
            Scope = scope,
            AllowedHostsValidator = new AllowedHostsValidator(validHosts),
        })
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(messageCallback);
    }
}
