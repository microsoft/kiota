using System.IO;

namespace Microsoft.Kiota.Abstractions.Serialization {
    public interface IParseNodeFactory {
        string ValidContentType { get; }
        IParseNode GetRootParseNode(string contentType, Stream content);
    }
}
