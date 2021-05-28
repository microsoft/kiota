using System;
using System.IO;
using System.Text.Json;
using Microsoft.Kiota.Abstractions.Serialization;

namespace Microsoft.Kiota.Serialization.Json {
    public class JsonParseNodeFactory : IParseNodeFactory
    {
        private const string validContentType = "application/json";
        public IParseNode GetRootParseNode(string contentType, Stream content)
        {
            if(string.IsNullOrEmpty(contentType))
                throw new ArgumentNullException(nameof(contentType));
            else if(!validContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentOutOfRangeException($"expected a {validContentType} content type");
            
            _ = content ?? throw new ArgumentNullException(nameof(content));

            using var jsonDocument = JsonDocument.Parse(content);
            return new JsonParseNode(jsonDocument.RootElement.Clone());
        }
    }
}
