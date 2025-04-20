using System;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.AL;

internal static class CodeIndexerExtensions
{
    public static CodeMethod ToCodeMethod(this CodeIndexer indexer)
    {
        ArgumentNullException.ThrowIfNull(indexer);
        var clonedType = (CodeType)indexer.ReturnType.Clone();
        var method = new CodeMethod
        {
            Name = "AAAItem_Idx",
            SimpleName = "Item_Idx",
            Access = AccessModifier.Public,
            ReturnType = clonedType,
            Kind = CodeMethodKind.Custom
        };
        method.SetSource("from indexer");
        method.AddCustomProperty("return-variable-name", "Rqst");
        method.AddParameter(indexer.IndexParameter);
        return method;
    }
}