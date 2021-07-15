namespace Kiota.Builder.Writers {
    public interface ILanguageConventionService
    {
        string GetAccessModifier(AccessModifier access);
        string StreamTypeName {get; }
        string VoidTypeName {get; }
        string DocCommentPrefix {get; }
        string PathSegmentPropertyName {get; }
        string CurrentPathPropertyName {get; }
        string HttpCorePropertyName {get; }
        string ParseNodeInterfaceName {get; }
        string GetTypeString(CodeTypeBase code);
        string TranslateType(string typeName);
        string GetParameterSignature(CodeParameter parameter);
        void WriteShortDescription(string description, LanguageWriter writer);
    }
}
