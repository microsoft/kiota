using System.Threading.Tasks;
using System.IO;

namespace Kiota.Abstractions {
    public interface IHttpCore<NativeResponseType> {
        Task<Stream> SendAsync(RequestInfo requestInfo);
        Task<NativeResponseType> SendNativAsync(RequestInfo requestInfo);
    }
}
