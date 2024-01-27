using System;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Dart;

public class DartConventionService : CommonLanguageConventionService
{
    public override string GetAccessModifier(AccessModifier access)
    {
        return access switch
        {
            AccessModifier.Private => "_",
            AccessModifier.Protected => "",
            AccessModifier.Public => "",
            _ => throw new ArgumentOutOfRangeException(nameof(access), access, null),
        };
    }

    public override string TranslateType(CodeType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.Name;
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
        get => "Stream";
    }

    public override string VoidTypeName => "void";

    public override string DocCommentPrefix => "///";

    public override string ParseNodeInterfaceName
    {
        get => "IParseNode";
    }

    public override string TempDictionaryVarName
    {
        get => "tempDictionary";
    }
}
