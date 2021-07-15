using System.Threading.Tasks;

namespace Microsoft.Kiota.Abstractions {
    /// <summary>
    ///     Defines the contract for a response handler.
    /// </summary>
    public interface IResponseHandler {
        /// <summary>
        ///     Callback method that is invoked when a response is received.
        /// </summary>
        /// <param name="response">The native response object.</param>
        /// <typeparam name="NativeResponseType">The type of the native response object.</typeparam>
        /// <typeparam name="ModelType">The type of the response model object.</typeparam>
        /// <returns>A task that represents the asynchronous operation and contains the deserialized response.</returns>
        Task<ModelType> HandleResponseAsync<NativeResponseType, ModelType>(NativeResponseType response);
    }
}
