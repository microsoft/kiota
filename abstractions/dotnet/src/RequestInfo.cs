using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Kiota.Abstractions.Serialization;

namespace Microsoft.Kiota.Abstractions
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
