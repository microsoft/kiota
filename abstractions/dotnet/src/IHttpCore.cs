using System.Threading.Tasks;
using System.IO;

namespace Kiota.Abstractions {
    public interface IHttpCore {
        Task<Stream> SendAsync(RequestInfo requestInfo);
        Task<NativeResponseType> SendNativAsync<NativeResponseType>(RequestInfo requestInfo);
    }
}
