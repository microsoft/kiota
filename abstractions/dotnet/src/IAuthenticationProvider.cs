using System;
using System.Threading.Tasks;

namespace Microsoft.Kiota.Abstractions {
    public interface IAuthenticationProvider {
        Task<string> GetAuthorizationToken(Uri requestUri);
    }
}
