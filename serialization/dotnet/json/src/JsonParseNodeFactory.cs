using System;
using System.IO;
using System.Text.Json;
using Microsoft.Kiota.Abstractions.Serialization;

namespace Microsoft.Kiota.Serialization.Json {
    public class JsonParseNodeFactory : IParseNodeFactory
    {
        public string ValidContentType { get; } = "application/json";
        public IParseNode GetRootParseNode(string contentType, Stream content)
        {
            if(string.IsNullOrEmpty(contentType))
                throw new ArgumentNullException(nameof(contentType));
            else if(!ValidContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentOutOfRangeException($"expected a {ValidContentType} content type");
            
            _ = content ?? throw new ArgumentNullException(nameof(content));

            using var jsonDocument = JsonDocument.Parse(content);
            return new JsonParseNode(jsonDocument.RootElement.Clone());
        }
    }
}
