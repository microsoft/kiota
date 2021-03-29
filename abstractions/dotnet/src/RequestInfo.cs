using System;
using System.Collections.Generic;
using System.IO;
using Kiota.Abstractions.Serialization;

namespace Kiota.Abstractions
{
    public class RequestInfo
    {
        public Uri URI { get; set; }
        public HttpMethod HttpMethod { get; set; }
        public IDictionary<string, object> QueryParameters { get; set; } = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        public Stream Content { get; set; }
        private const string jsonContentType = "application/json";
        private const string binaryContentType = "application/octet-stream";
        private const string contentTypeHeader = "Content-Type";
        public void SetStreamContent(Stream content) {
            Content = content;
            Headers.Add(contentTypeHeader, binaryContentType);
        }
        public void SetJsonContentFromParsable<T>(T item, Func<string, ISerializationWriter> writerFactory) where T : class, IParsable<T>, new() {
            if(writerFactory != null) {
                using var writer = writerFactory.Invoke(jsonContentType);
                writer.WriteObjectValue(null, item);
                Headers.Add(contentTypeHeader, jsonContentType);
                Content = writer.GetSerializedContent();
            }
        }
    }
}
