using System;
using System.Threading.Tasks;

namespace Microsoft.Kiota.Abstractions {
    /// <summary>
    /// Authenticates the application and returns a token.
    /// </summary>
    public interface IAuthenticationProvider {
        /// <summary>
        /// Authenticates the application and returns a token base on the provided Uri.
        /// </summary>
        /// <param name="uri">The Uri to authenticate the request for.</param>
        /// <returns>The Access Token or null if the target request Uri doesn't correspond to a valid resource.</returns>
        Task<string> GetAuthorizationToken(Uri requestUri);
    }
}
