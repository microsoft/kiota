namespace Kiota.Abstractions.Serialization {
    public interface IParseNode {
        string GetStringValue();
        IParseNode GetChildNode(string identifier);
    }
}
