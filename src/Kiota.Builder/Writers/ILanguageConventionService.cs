using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers;

public interface ILanguageConventionService
{
    string GetAccessModifier(AccessModifier access);
    string StreamTypeName
    {
        get;
    }
    string VoidTypeName
    {
        get;
    }
    string DocCommentPrefix
    {
        get;
    }
    string ParseNodeInterfaceName
    {
        get;
    }
    string TempDictionaryVarName
    {
        get;
    }
    string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true, LanguageWriter? writer = null);
    string TranslateType(CodeType type);
    string GetParameterSignature(CodeParameter parameter, CodeElement targetElement, LanguageWriter? writer = null);
    bool WriteShortDescription(IDocumentedElement element, LanguageWriter writer, string prefix = "", string suffix = "");
}
