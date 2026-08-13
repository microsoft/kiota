using System;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers;

public class RelativeImportManager
{
    protected string prefix
    {
        get; init;
    }
    protected char separator
    {
        get; init;
    }
    private readonly Func<CodeNamespace, CodeElement, string>? NormalizeFileNameCallback;
    public RelativeImportManager(string namespacePrefix, char namespaceSeparator, Func<CodeNamespace, CodeElement, string>? normalizeFileNameCallback = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(namespacePrefix);
        if (namespaceSeparator == default)
            throw new ArgumentNullException(nameof(namespaceSeparator));

        prefix = namespacePrefix;
        separator = namespaceSeparator;
        NormalizeFileNameCallback = normalizeFileNameCallback;
    }
    /// <summary>
    /// Returns the relative import path for the given using and import context namespace.
    /// </summary>
    /// <param name="codeUsing">The using to import into the current namespace context</param>
    /// <param name="currentNamespace">The current namespace</param>
    /// <returns>The import symbol, it's alias if any and the relative import path</returns>
    public virtual (string, string, string) GetRelativeImportPathForUsing(CodeUsing codeUsing, CodeNamespace currentNamespace)
    {
        if (codeUsing?.IsExternal ?? true)
            return (string.Empty, string.Empty, string.Empty);//it's an external import, add nothing

        var (importSymbol, typeDef) = codeUsing.Declaration?.TypeDefinition is CodeElement td ? td switch
        {
            CodeFunction f => (f.Name.ToFirstCharacterLowerCase(), td),
            _ => (td.Name.ToFirstCharacterUpperCase(), td),
        } : (codeUsing.Name, null);

        if (typeDef == null)
            return (importSymbol, codeUsing.Alias, "./"); // it's relative to the folder, with no declaration (default failsafe)
        var importPath = GetImportRelativePathFromNamespaces(currentNamespace,
            typeDef.GetImmediateParentOfType<CodeNamespace>());
        importPath += NormalizeFileNameCallback == null ?
                        (string.IsNullOrEmpty(importPath) ? codeUsing.Name : codeUsing.Declaration!.Name.ToFirstCharacterLowerCase()) :
                        NormalizeFileNameCallback(codeUsing.Declaration!.TypeDefinition!.GetImmediateParentOfType<CodeNamespace>(), codeUsing.Declaration);
        return (importSymbol, codeUsing.Alias, importPath);
    }
    protected string GetImportRelativePathFromNamespaces(CodeNamespace currentNamespace, CodeNamespace importNamespace)
    {
        ArgumentNullException.ThrowIfNull(currentNamespace);
        var result = currentNamespace.GetDifferential(importNamespace, prefix, separator);
        return result.State switch
        {
            NamespaceDifferentialTrackerState.Same => "./",
            NamespaceDifferentialTrackerState.Downwards => $"./{GetRemainingImportPath(result.DownwardsSegments)}",
            NamespaceDifferentialTrackerState.Upwards => GetUpwardsMoves(result.UpwardsMovesCount),
            NamespaceDifferentialTrackerState.UpwardsAndThenDownwards => $"{GetUpwardsMoves(result.UpwardsMovesCount)}{GetRemainingImportPath(result.DownwardsSegments)}",
            _ => throw new NotImplementedException(),
        };
    }
    private const char PathSeparator = '/';
    protected static string GetUpwardsMoves(int UpwardsMovesCount) => string.Join(PathSeparator, Enumerable.Repeat("..", UpwardsMovesCount)) + (UpwardsMovesCount > 0 ? PathSeparator : string.Empty);
    protected static string GetRemainingImportPath(IEnumerable<string> remainingSegments)
    {
        var segments = remainingSegments.Select(x => x.ToFirstCharacterLowerCase()).ToArray();
        if (segments.Length != 0)
            return segments.Aggregate(static (x, y) => $"{x}/{y}") + PathSeparator;
        return string.Empty;
    }
}
