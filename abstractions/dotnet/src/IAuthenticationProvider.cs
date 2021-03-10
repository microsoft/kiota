using System;
using System.Threading.Tasks;

namespace Kiota.Abstractions {
    public interface IAuthenticationProvider {
        Task<string> getAuthorizationToken(Uri requestUri);
    }
}
