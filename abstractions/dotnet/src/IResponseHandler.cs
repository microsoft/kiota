using System.Threading.Tasks;

namespace Microsoft.Kiota.Abstractions {
    public interface IResponseHandler {
        Task<ModelType> HandleResponseAsync<NativeResponseType, ModelType>(NativeResponseType response);
    }
}
