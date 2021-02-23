using System.Threading.Tasks;
using System.IO;

namespace kiota.core {
    public interface IHttpCore<NativeResponseType> {
        Task<Stream> SendAsync(RequestInfo requestInfo);
        Task<NativeResponseType> SendNativAsync(RequestInfo requestInfo);
    }
}
