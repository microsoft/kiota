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
        public IDictionary<string, object> QueryParameters { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Stream Content { get; set; }
        private const string binaryContentType = "application/octet-stream";
        private const string contentTypeHeader = "Content-Type";
        public void SetStreamContent(Stream content) {
            Content = content;
            Headers.Add(contentTypeHeader, binaryContentType);
        }
        public void SetContentFromParsable<T>(T item, ISerializationWriterFactory writerFactory, string contentType) where T : class, IParsable<T>, new() {
            if(string.IsNullOrEmpty(contentType)) throw new ArgumentNullException(nameof(contentType));
            if(writerFactory == null) throw new ArgumentNullException(nameof(writerFactory));

            using var writer = writerFactory.GetSerializationWriter(contentType);
            writer.WriteObjectValue(null, item);
            Headers.Add(contentTypeHeader, contentType);
            Content = writer.GetSerializedContent();
        }
    }
}
