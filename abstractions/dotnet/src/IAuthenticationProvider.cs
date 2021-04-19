using System;
using System.Threading.Tasks;

namespace Kiota.Abstractions {
    public interface IAuthenticationProvider {
        Task<string> GetAuthorizationToken(Uri requestUri);
    }
}
