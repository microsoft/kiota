using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Dart;

public class DartConventionService : CommonLanguageConventionService
{
    public override string GetAccessModifier(AccessModifier access)
    {
        throw new System.NotImplementedException();
    }

    public override string TranslateType(CodeType type)
    {
        throw new System.NotImplementedException();
    }

    public override void WriteShortDescription(string description, LanguageWriter writer)
    {
        throw new System.NotImplementedException();
    }

    public override string GetParameterSignature(CodeParameter parameter, CodeElement targetElement, LanguageWriter? writer = null)
    {
        throw new System.NotImplementedException();
    }

    public override string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true,
        LanguageWriter? writer = null)
    {
        throw new System.NotImplementedException();
    }

    public override string StreamTypeName
    {
        get;
    }

    public override string VoidTypeName => "void";

    public override string DocCommentPrefix => "///";

    public override string ParseNodeInterfaceName
    {
        get;
    }

    public override string TempDictionaryVarName
    {
        get;
    }
}
