using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.TypeScript;
public class CodeUsingWriter {
    private readonly TypescriptRelativeImportManager _relativeImportManager;
    public CodeUsingWriter(string clientNamespaceName)
    {
        _relativeImportManager = new TypescriptRelativeImportManager(clientNamespaceName, '.');
    }
    public void WriteCodeElement(IEnumerable<CodeUsing> usings, CodeNamespace parentNamespace, LanguageWriter writer ) {
        var externalImportSymbolsAndPaths = usings
                                                .Where(static x => x.IsExternal)
                                                .Select(static x => (x.Name, string.Empty, x.Declaration?.Name));
        var internalImportSymbolsAndPaths = usings
                                                .Where(static x => !x.IsExternal)
                                                .Select(x => _relativeImportManager.GetRelativeImportPathForUsing(x, parentNamespace));
        var importSymbolsAndPaths = externalImportSymbolsAndPaths.Union(internalImportSymbolsAndPaths)
                                                                .GroupBy(static x => x.Item3)
                                                                .OrderBy(static x => x.Key);
        foreach (var codeUsing in importSymbolsAndPaths.Where(static x => !string.IsNullOrWhiteSpace(x.Key)))
            writer.WriteLine($"import {{{codeUsing.Select(static x => GetAliasedSymbol(x.Item1, x.Item2)).Distinct().OrderBy(static x => x).Aggregate(static (x, y) => x + ", " + y)}}} from '{codeUsing.Key}';");

        writer.WriteLine();
    }
    private static string GetAliasedSymbol(string symbol, string alias) {
        return string.IsNullOrEmpty(alias) ? symbol : $"{symbol} as {alias}";
    }
}
