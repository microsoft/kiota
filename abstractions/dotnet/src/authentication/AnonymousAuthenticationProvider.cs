using System;
using System.Threading.Tasks;

namespace Microsoft.Kiota.Abstractions.Authentication {
    /// <summary>
    /// This authentication provider does not perform any authentication.
    /// </summary>
    public class AnonymousAuthenticationProvider : IAuthenticationProvider {
        public Task AuthenticateRequest(RequestInfo request) {
            return Task.CompletedTask;
        }
    }
}
