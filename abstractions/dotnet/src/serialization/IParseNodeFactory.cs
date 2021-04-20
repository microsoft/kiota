using System.IO;

namespace Kiota.Abstractions.Serialization {
    public interface IParseNodeFactory {
        IParseNode GetRootParseNode(string contentType, Stream content);
    }
}
