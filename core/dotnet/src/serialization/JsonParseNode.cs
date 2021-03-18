using System;
using System.Text.Json;
using Kiota.Abstractions.Serialization;

namespace KiotaCore.Serialization {
    public class JsonParseNode : IParseNode {
        private readonly JsonElement _jsonNode;
        public JsonParseNode(JsonElement node)
        {
            _jsonNode = node;            
        }
        public string GetStringValue() => _jsonNode.GetString();
        public IParseNode GetChildNode(string identifier) => new JsonParseNode(_jsonNode.GetProperty(identifier ?? throw new ArgumentNullException(nameof(identifier))));
    }
}
