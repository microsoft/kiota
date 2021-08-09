using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Microsoft.Kiota.Abstractions.Serialization;

namespace Microsoft.Kiota.Abstractions
{
    /// <summary>
    ///     This class represents an abstract HTTP request.
    /// </summary>
    public class RequestInfo
    {
        /// <summary>
        ///  The URI of the request.
        /// </summary>
        public Uri URI { get; set; }
        /// <summary>
        ///  The <see cref="HttpMethod">HTTP method</see> of the request.
        /// </summary>
        public HttpMethod HttpMethod { get; set; }
        /// <summary>
        /// The Query Parameters of the request.
        /// </summary>
        public IDictionary<string, object> QueryParameters { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// The Request Headers.
        /// </summary>
        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// The Request Body.
        /// </summary>
        public Stream Content { get; set; }
        private Dictionary<string, IMiddlewareOption> _middlewareOptions = new Dictionary<string, IMiddlewareOption>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Gets the middleware options for this request. Options are unique by type. If an option of the same type is added twice, the last one wins.
        /// </summary>
        public IEnumerable<IMiddlewareOption> MiddlewareOptions { get { return _middlewareOptions.Values; } }
        /// <summary>
        /// Adds a middleware option to the request.
        /// </summary>
        /// <param name="middlewareOption">The middleware option to add.</param>
        public void AddMiddlewareOptions(params IMiddlewareOption[] options) {
            if(!options?.Any() ?? false) return; // it's a no-op if there are no options and this avoid having to check in the code gen.
            foreach(var option in options.Where(x => x != null))
                if(!_middlewareOptions.TryAdd(option.GetType().FullName, option))
                    _middlewareOptions[option.GetType().FullName] = option;
        }
        /// <summary>
        /// Removes given middleware options from the current request.
        /// </summary>
        /// <param name="options">Middleware options to remove.</param>
        public void RemoveMiddlewareOptions(params IMiddlewareOption[] options) {
            if(!options?.Any() ?? false) throw new ArgumentNullException(nameof(options));
            foreach(var optionName in options.Where(x => x != null).Select(x => x.GetType().FullName))
                _middlewareOptions.Remove(optionName);
        }
        private const string binaryContentType = "application/octet-stream";
        private const string contentTypeHeader = "Content-Type";
        /// <summary>
        /// Sets the request body to a binary stream.
        /// </summary>
        /// <param name="content">The binary stream to set as a body.</param>
        public void SetStreamContent(Stream content) {
            Content = content;
            Headers.Add(contentTypeHeader, binaryContentType);
        }
        /// <summary>
        /// Sets the request body from a model with the specified content type.
        /// </summary>
        /// <param name="coreService">The core service to get the serialization writer from.</param>
        /// <param name="item">The model to serialize.</param>
        /// <param name="contentType">The content type to set.</param>
        /// <typeparam name="T">The model type to serialize.</typeparam>
        public void SetContentFromParsable<T>(T item, IHttpCore coreService, string contentType) where T : IParsable {
            if(string.IsNullOrEmpty(contentType)) throw new ArgumentNullException(nameof(contentType));
            if(coreService == null) throw new ArgumentNullException(nameof(coreService));

            using var writer = coreService.SerializationWriterFactory.GetSerializationWriter(contentType);
            writer.WriteObjectValue(null, item);
            Headers.Add(contentTypeHeader, contentType);
            Content = writer.GetSerializedContent();
        }
    }
}
