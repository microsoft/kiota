using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions.Serialization;

namespace Microsoft.Kiota.Abstractions {
    public interface IHttpCore {
        Task<ModelType> SendAsync<ModelType>(RequestInfo requestInfo, IResponseHandler responseHandler = default) where ModelType : IParsable;
        Task<ModelType> SendPrimitiveAsync<ModelType>(RequestInfo requestInfo, IResponseHandler responseHandler = default);
        Task SendNoContentAsync(RequestInfo requestInfo, IResponseHandler responseHandler = default);
    }
}
