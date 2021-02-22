using System;
using System.Collections.Generic;
using System.IO;

namespace kiota.core
{
    public class RequestInfo
    {
        public Uri URI { get; set; }
        public HttpMethod HttpMethod { get; set; }
        public IDictionary<string, object> QueryParameters { get; set; } = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        public Stream Content { get; set; }
    }
}
