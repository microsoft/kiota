using System.Threading.Tasks;
using Kiota.Abstractions.Serialization;

namespace Kiota.Abstractions {
    public interface IHttpCore {
        Task<ModelType> SendAsync<ModelType>(RequestInfo requestInfo, IResponseHandler responseHandler = default) where ModelType : class, IParsable<ModelType>, new();
        Task SendAsync(RequestInfo requestInfo, IResponseHandler responseHandler = default);
    }
}
