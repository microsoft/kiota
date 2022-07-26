using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder.Writers.TypeScript;
public class CodeUsingWriter {
    private readonly TypescriptRelativeImportManager _relativeImportManager;
    public CodeUsingWriter(string clientNamespaceName)
    {
        _relativeImportManager = new TypescriptRelativeImportManager(clientNamespaceName, '.');
    }
    public void WriteCodeElement(IEnumerable<CodeUsing> usings, CodeNamespace parentNamespace, LanguageWriter writer ) {
        var externalImportSymbolsAndPaths = usings
                                                .Where(x => x.IsExternal)
                                                .Select(x => (x.Name, string.Empty, x.Declaration?.Name));
        var internalImportSymbolsAndPaths = usings
                                                .Where(x => !x.IsExternal)
                                                .Select(x => _relativeImportManager.GetRelativeImportPathForUsing(x, parentNamespace));
        var importSymbolsAndPaths = externalImportSymbolsAndPaths.Union(internalImportSymbolsAndPaths)
                                                                .GroupBy(x => x.Item3)
                                                                .OrderBy(x => x.Key);
        foreach (var codeUsing in importSymbolsAndPaths)
            if (!string.IsNullOrWhiteSpace(codeUsing.Key))
            {
                writer.WriteLine($"import {{{codeUsing.Select(x => GetAliasedSymbol(x.Item1, x.Item2)).Distinct().OrderBy(x => x).Aggregate((x, y) => x + ", " + y)}}} from '{codeUsing.Key}';");
            }

        writer.WriteLine();
    }
    private static string GetAliasedSymbol(string symbol, string alias) {
        return string.IsNullOrEmpty(alias) ? symbol : $"{symbol} as {alias}";
    }
}
