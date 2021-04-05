using System.Threading.Tasks;

namespace Kiota.Abstractions {
    public interface IResponseHandler {
        Task<ModelType> HandleResponseAsync<NativeResponseType, ModelType>(NativeResponseType response);
    }
}
