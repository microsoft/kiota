using System;
using Kiota.Builder.CodeDOM;
using static Kiota.Builder.CodeDOM.CodeTypeBase;

namespace Kiota.Builder.Writers.AL;

internal static class CodeTypeBaseExtensions
{
    public static CodeType GetTypeFromBase(this CodeTypeBase codeType)
    {
        return (CodeType)codeType;
    }
    public static string GetNamespaceName(this CodeTypeBase codeType)
    {
        ArgumentNullException.ThrowIfNull(codeType);
        var definition = codeType.GetTypeFromBase().TypeDefinition;
        if (definition is null)
            return string.Empty;
        return definition.GetImmediateParentOfType<CodeNamespace>()?.Name ?? string.Empty;
    }
    public static CodeTypeBase CloneWithoutCollection(this CodeTypeBase codeType)
    {
        ArgumentNullException.ThrowIfNull(codeType);
        var newType = (CodeType)codeType.Clone();
        newType.CollectionKind = CodeTypeCollectionKind.None;
        return newType;
    }
}