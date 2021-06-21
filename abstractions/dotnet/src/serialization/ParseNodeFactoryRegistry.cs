using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Kiota.Abstractions;

namespace Microsoft.Kiota.Abstractions.Serialization {
    public class ParseNodeFactoryRegistry : IParseNodeFactory {
        public static readonly ParseNodeFactoryRegistry DefaultInstance = new();
        public Dictionary<string, IParseNodeFactory> ContentTypeAssociatedFactories {get; set;} = new Dictionary<string, IParseNodeFactory>();
        public IParseNode GetRootParseNode(string contentType, Stream content) {
            if(string.IsNullOrEmpty(contentType))
                throw new ArgumentNullException(nameof(contentType));
            _ = content ?? throw new ArgumentNullException(nameof(content));

            if(ContentTypeAssociatedFactories.ContainsKey(contentType))
                return ContentTypeAssociatedFactories[contentType].GetRootParseNode(contentType, content);
            else
                throw new InvalidOperationException($"Content type {contentType} does not have a factory registered to be parsed");
        }
    }
}
