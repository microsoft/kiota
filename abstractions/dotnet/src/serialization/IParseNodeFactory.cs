using System.IO;

namespace Microsoft.Kiota.Abstractions.Serialization {
    public interface IParseNodeFactory {
        IParseNode GetRootParseNode(string contentType, Stream content);
    }
}
