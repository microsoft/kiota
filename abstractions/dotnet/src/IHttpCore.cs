using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions.Serialization;

namespace Microsoft.Kiota.Abstractions {
    /// <summary>
    ///   Service responsible for translating abstract Request Info into concrete native HTTP requests.
    /// </summary>
    public interface IHttpCore {
        /// <summary>
        ///  Enables the backing store proxies for the SerializationWriters and ParseNodes in use.
        /// </summary>
        void EnableBackingStore();
        /// <summary>
        /// Gets the serialization writer factory currently in use for the HTTP core service.
        /// </summary>
        ISerializationWriterFactory SerializationWriterFactory { get; }
        /// <summary>
        /// Excutes the HTTP request specified by the given RequestInfo and returns the deserialized response model.
        /// </summary>
        /// <param name="requestInfo">The RequestInfo object to use for the HTTP request.</param>
        /// <param name="responseHandler">The response handler to use for the HTTP request instead of the default handler.</param>
        /// <returns>The deserialized response model.</returns>
        Task<ModelType> SendAsync<ModelType>(RequestInfo requestInfo, IResponseHandler responseHandler = default) where ModelType : IParsable;
        /// <summary>
        /// Excutes the HTTP request specified by the given RequestInfo and returns the deserialized primitive response model.
        /// </summary>
        /// <param name="requestInfo">The RequestInfo object to use for the HTTP request.</param>
        /// <param name="responseHandler">The response handler to use for the HTTP request instead of the default handler.</param>
        /// <returns>The deserialized primitive response model.</returns>
        Task<ModelType> SendPrimitiveAsync<ModelType>(RequestInfo requestInfo, IResponseHandler responseHandler = default);
        /// <summary>
        /// Excutes the HTTP request specified by the given RequestInfo with no return content.
        /// </summary>
        /// <param name="requestInfo">The RequestInfo object to use for the HTTP request.</param>
        /// <param name="responseHandler">The response handler to use for the HTTP request instead of the default handler.</param>
        /// <returns>A Task to await completion.</returns>
        Task SendNoContentAsync(RequestInfo requestInfo, IResponseHandler responseHandler = default);
    }
}
