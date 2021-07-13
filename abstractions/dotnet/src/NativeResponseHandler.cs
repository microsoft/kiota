using System.Threading.Tasks;

namespace Microsoft.Kiota.Abstractions {
    /// <summary>
    /// Default response handler to access the native response object.
    /// </summary>
    public class NativeResponseHandler : IResponseHandler {
        public object Value;
        public Task<ModelType> HandleResponseAsync<NativeResponseType, ModelType>(NativeResponseType response) {
            Value = response;
            return Task.FromResult(default(ModelType));
        }
    }
}
