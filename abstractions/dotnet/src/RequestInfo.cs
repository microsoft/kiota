using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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
        public async Task SetContentFromParsable<T>(T item, ISerializationWriter writer) where T : class, IParsable<T>, new() {
            writer.WriteObjectValue(null, item);
            Content = await writer.GetSerializedContent();
        }
    }
}
