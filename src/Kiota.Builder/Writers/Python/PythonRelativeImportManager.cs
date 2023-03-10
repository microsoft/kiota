using System;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Python;
public class PythonRelativeImportManager : RelativeImportManager
{
    public PythonRelativeImportManager(string namespacePrefix, char namespaceSeparator) : base(namespacePrefix, namespaceSeparator)
    {
    }
    /// <summary>
    /// Returns the relative import path for the given using and import context namespace.
    /// </summary>
    /// <param name="codeUsing">The using to import into the current namespace context</param>
    /// <param name="currentNamespace">The current namespace</param>
    /// <returns>The import symbol, it's alias if any and the relative import path</returns>
    public override (string, string, string) GetRelativeImportPathForUsing(CodeUsing codeUsing, CodeNamespace currentNamespace)
    {
        if (codeUsing?.IsExternal ?? true)
            return (string.Empty, string.Empty, string.Empty);//it's an external import, add nothing
        var (importSymbol, typeDef) = codeUsing.Declaration?.TypeDefinition is CodeElement td ? td switch
        {
            CodeFunction f => (f.Name.ToFirstCharacterLowerCase(), td),
            _ => (td.Name.ToSnakeCase(), td),
        } : (codeUsing.Name, null);

        if (typeDef == null)
            return (importSymbol, codeUsing.Alias, "."); // it's relative to the folder, with no declaration (default failsafe)
        var importPath = GetImportRelativePathFromNamespaces(currentNamespace,
            typeDef.GetImmediateParentOfType<CodeNamespace>());
        return (importSymbol, codeUsing.Alias, importPath);
    }
    protected new string GetImportRelativePathFromNamespaces(CodeNamespace currentNamespace, CodeNamespace importNamespace)
    {
        var result = currentNamespace.GetDifferential(importNamespace, prefix, separator);
        return result.State switch
        {
            NamespaceDifferentialTrackerState.Same => ".",
            NamespaceDifferentialTrackerState.Downwards => $".{GetRemainingImportPath(result.DownwardsSegments)}",
            NamespaceDifferentialTrackerState.Upwards => GetUpwardsMoves(result.UpwardsMovesCount),
            NamespaceDifferentialTrackerState.UpwardsAndThenDownwards => $"{GetUpwardsMoves(result.UpwardsMovesCount)}{GetRemainingImportPath(result.DownwardsSegments)}",
            _ => throw new NotImplementedException(),
        };
    }
    protected static new string GetUpwardsMoves(int UpwardsMovesCount) => string.Join("", Enumerable.Repeat(".", UpwardsMovesCount)) + (UpwardsMovesCount > 0 ? "." : string.Empty);
    protected static new string GetRemainingImportPath(IEnumerable<string> remainingSegments)
    {
        if (remainingSegments.Any())
            return remainingSegments.Select(x => x.ToFirstCharacterLowerCase()).Aggregate((x, y) => $"{x}.{y}");
        return string.Empty;
    }
}
