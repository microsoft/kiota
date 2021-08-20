using System;
using System.Threading.Tasks;

namespace Microsoft.Kiota.Abstractions.Authentication {
    /// <summary>
    /// Authenticates the application request.
    /// </summary>
    public interface IAuthenticationProvider {
        /// <summary>
        /// Authenticates the application request.
        /// </summary>
        /// <param name="request">The request to authenticate.</param>
        /// <returns>A task to await for the authentication to be completed.</returns>
        Task AuthenticateRequestAsync(RequestInfo request);
    }
}
