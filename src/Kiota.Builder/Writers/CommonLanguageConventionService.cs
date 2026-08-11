using System;
using System.Linq;

using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers;

public abstract class CommonLanguageConventionService : ILanguageConventionService
{
    public abstract string StreamTypeName
    {
        get;
    }
    public abstract string VoidTypeName
    {
        get;
    }
    public abstract string DocCommentPrefix
    {
        get;
    }
    public abstract string ParseNodeInterfaceName
    {
        get;
    }
    public abstract string TempDictionaryVarName
    {
        get;
    }

    public abstract string GetAccessModifier(AccessModifier access);
    public abstract string GetParameterSignature(CodeParameter parameter, CodeElement targetElement, LanguageWriter? writer = null);
    public abstract string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true, LanguageWriter? writer = null);

    public string TranslateType(CodeTypeBase type)
    {
        if (type is CodeType currentType)
            return TranslateType(currentType);
        if (type is CodeComposedTypeBase currentUnionType)
            return TranslateType(currentUnionType.AllTypes.First());
        throw new InvalidOperationException("Unknown type");
    }

    public abstract string TranslateType(CodeType type);
    public abstract bool WriteShortDescription(IDocumentedElement element, LanguageWriter writer, string prefix = "", string suffix = "");
}
