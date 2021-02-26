using System.Threading.Tasks;

namespace Kiota.Abstractions {
    public interface IHttpCore {
        Task<ModelType> SendAsync<ModelType>(RequestInfo requestInfo, IResponseHandler responseHandler = default);
    }
}
