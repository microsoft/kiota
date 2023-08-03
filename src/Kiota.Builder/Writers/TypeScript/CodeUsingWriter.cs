using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.TypeScript;
public class CodeUsingWriter
{
    private readonly TypescriptRelativeImportManager _relativeImportManager;
    public CodeUsingWriter(string clientNamespaceName)
    {
        _relativeImportManager = new TypescriptRelativeImportManager(clientNamespaceName, '.');
    }
    public void WriteCodeElement(IEnumerable<CodeUsing> usings, CodeNamespace parentNamespace, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        var enumeratedUsings = usings.ToArray();
        var externalImportSymbolsAndPaths = enumeratedUsings
                                                .Where(static x => x.IsExternal)
                                                .Select(static x => new { Symbol = x.Name, Alias = string.Empty, Path = x.Declaration?.Name ?? string.Empty, ShouldUseTypeImport = false });
        var internalImportSymbolsAndPaths = enumeratedUsings
                                                .Where(static x => !x.IsExternal)
                                                .Select(x => new { CodeUsingPathTokens = _relativeImportManager.GetRelativeImportPathForUsing(x, parentNamespace), ShouldUseTypeImport = GetShouldUseTypeImport(x) });
        var importSymbolsAndPaths = externalImportSymbolsAndPaths
                                                .Union(internalImportSymbolsAndPaths.Select(static x => new { Symbol = x.CodeUsingPathTokens.Item1, Alias = x.CodeUsingPathTokens.Item2, Path = x.CodeUsingPathTokens.Item3, x.ShouldUseTypeImport }))
                                                .GroupBy(static x => x.Path)
                                                .OrderBy(static x => x.Key);
        foreach (var codeUsing in importSymbolsAndPaths.Where(static x => !string.IsNullOrWhiteSpace(x.Key)))
            writer.WriteLine($"import {codeUsing.Select(x => x.ShouldUseTypeImport ? "type " : "").Distinct().OrderBy(static x => x, StringComparer.Ordinal).Aggregate(static (x, y) => x)}" +
                $"{{{codeUsing.Select(static x => GetAliasedSymbol(x.Symbol, x.Alias)).Distinct().OrderBy(static x => x, StringComparer.Ordinal).Aggregate(static (x, y) => x + ", " + y)}}} from '{codeUsing.Key}';");

        writer.WriteLine();
    }

    /**
    * Determines whether the import clause on TypeScript should include `import type` or just normal `import` statement
    *
    * @param codeUsing The code using statement to check
    **/
    private static bool GetShouldUseTypeImport(CodeUsing codeUsing)
    {
        // Check if codeUsing is Erassable or codeUsing.Declaration is an instance of CodeType and if codeType.TypeDefinition is an instance of CodeInterface
        return codeUsing.IsErasable || codeUsing.Declaration is CodeType codeType && codeType.TypeDefinition is CodeInterface;
    }

    private static string GetAliasedSymbol(string symbol, string alias)
    {
        return string.IsNullOrEmpty(alias) ? symbol : $"{symbol} as {alias}";
    }
}
