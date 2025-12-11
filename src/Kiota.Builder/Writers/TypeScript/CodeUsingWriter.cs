using System;
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
                                                .Select(static x => new { Symbol = x.Name, Alias = string.Empty, Path = x.Declaration?.Name ?? string.Empty, ShouldUseTypeImport = GetShouldUseTypeImport(x) });
        var internalImportSymbolsAndPaths = enumeratedUsings
                                                .Where(static x => !x.IsExternal)
                                                .Select(x => new { CodeUsingPathTokens = _relativeImportManager.GetRelativeImportPathForUsing(x, parentNamespace), ShouldUseTypeImport = GetShouldUseTypeImport(x) });
        var importSymbolsAndPaths = externalImportSymbolsAndPaths
                                                .Union(internalImportSymbolsAndPaths.Select(static x => new { Symbol = x.CodeUsingPathTokens.Item1, Alias = x.CodeUsingPathTokens.Item2, Path = x.CodeUsingPathTokens.Item3, x.ShouldUseTypeImport }))
                                                .GroupBy(static x => x.Path)
                                                .OrderBy(static x => x.Key);
        foreach (var codeUsing in importSymbolsAndPaths.Where(static x => !string.IsNullOrWhiteSpace(x.Key)))
        {
            writer.WriteLine("// @ts-ignore");
            writer.WriteLine($"import {{ {codeUsing.Select(static x => GetAliasedSymbol(x.Symbol, x.Alias, x.ShouldUseTypeImport)).Distinct().OrderBy(static x => x).Aggregate(static (x, y) => x + ", " + y)} }} from '{codeUsing.Key}';");
        }

        writer.WriteLine();
    }

    /**
    * Determines whether the import clause on TypeScript should include `import type` or just normal `import` statement
    * @param codeUsing The code using statement to check
    **/
    private static bool GetShouldUseTypeImport(CodeUsing codeUsing)
    {
        // Check if codeUsing is Erassable
        if (codeUsing.IsErasable) return true;
        if (codeUsing.Declaration is CodeType codeType)
        {
            if (codeType.TypeDefinition is CodeInterface) return true;
            // this will handle edge cases for typescript Declarations that are already known to be interfaces: RequestConfiguration, QueryParameters, and Model classes
            if (codeType.TypeDefinition is CodeClass codeClass && codeClass.IsOfKind(CodeClassKind.RequestConfiguration, CodeClassKind.QueryParameters, CodeClassKind.Model)) return true;
        }
        return false;
    }

    private static string GetAliasedSymbol(string symbol, string alias, bool shouldUseTypeImport)
    {
        var importPrefix = shouldUseTypeImport ? "type " : "";
        return string.IsNullOrEmpty(alias) ? $"{importPrefix}{symbol}" : $"{importPrefix}{symbol} as {alias}";
    }
}
