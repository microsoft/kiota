using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;


namespace Kiota.Builder.Writers.Python;
public class CodeUsingWriter
{
    private readonly PythonRelativeImportManager _relativeImportManager;
    public CodeUsingWriter(string clientNamespaceName)
    {
        _relativeImportManager = new PythonRelativeImportManager(clientNamespaceName, '.');
    }
    /// <summary>
    /// Writes external imports for a given code element.
    /// </summary>
    /// <param name="codeElement">The element to write external usings</param>
    /// <param name="writer">An instance of the language writer</param>
    /// <returns>void</returns>
    public void WriteExternalImports(ClassDeclaration codeElement, LanguageWriter writer)
    {
        var externalImportSymbolsAndPaths = codeElement.Usings
                                                        .Where(static x => x.IsExternal)
                                                        .Select(x => (x.Name, string.Empty, x.Declaration?.Name))
                                                        .GroupBy(x => x.Item3)
                                                        .OrderBy(x => x.Key);
        if (externalImportSymbolsAndPaths.Any())
        {
            foreach (var codeUsing in externalImportSymbolsAndPaths)
                if (!string.IsNullOrWhiteSpace(codeUsing.Key))
                {
                    if (codeUsing.Key == "-")
                        writer.WriteLine($"import {codeUsing.Select(x => GetAliasedSymbol(x.Item1, x.Item2)).Distinct().OrderBy(x => x).Aggregate((x, y) => x + ", " + y)}");
                    else
                        writer.WriteLine($"from {codeUsing.Key.ToSnakeCase()} import {codeUsing.Select(x => GetAliasedSymbol(x.Item1, x.Item2)).Distinct().OrderBy(x => x).Aggregate((x, y) => x + ", " + y)}");
                }
            writer.WriteLine();
        }
    }
    /// <summary>
    /// Writes error mapping imports for a given code class.
    /// </summary>
    /// <param name="parentClass">The CodeClass from which to write error mapping usings</param>
    /// <param name="writer">An instance of the language writer</param>
    /// <returns>void</returns>
    public void WriteInternalErrorMappingImports(CodeClass parentClass, LanguageWriter writer)
    {
        var parentNameSpace = parentClass.GetImmediateParentOfType<CodeNamespace>();
        var internalErrorMappingImportSymbolsAndPaths = parentClass.Usings
                                                        .Where(x => !x.IsExternal)
                                                        .Where(x => x.Declaration?.TypeDefinition is CodeClass codeClass && codeClass.IsErrorDefinition)
                                                        .Select(x => _relativeImportManager.GetRelativeImportPathForUsing(x, parentNameSpace))
                                                        .GroupBy(x => x.Item3)
                                                        .Where(x => !string.IsNullOrEmpty(x.Key))
                                                        .OrderBy(x => x.Key);
        WriteCodeUsings(internalErrorMappingImportSymbolsAndPaths, writer);
    }
    /// <summary>
    /// Writes all internal imports for a given code class.
    /// </summary>
    /// <param name="parentClass">The CodeClass from which to write internal usings</param>
    /// <param name="writer">An instance of the language writer</param>
    /// <returns>void</returns>
    public void WriteInternalImports(CodeClass parentClass, LanguageWriter writer)
    {
        var parentNameSpace = parentClass.GetImmediateParentOfType<CodeNamespace>();
        var internalImportSymbolsAndPaths = parentClass.Usings
                                                        .Where(x => !x.IsExternal)
                                                        .Select(x => _relativeImportManager.GetRelativeImportPathForUsing(x, parentNameSpace))
                                                        .GroupBy(x => x.Item3)
                                                        .Where(x => !string.IsNullOrEmpty(x.Key))
                                                        .OrderBy(x => x.Key);
        WriteCodeUsings(internalImportSymbolsAndPaths, writer);
    }
    /// <summary>
    /// Writes conditional internal imports for a given code element for type checking environments.
    /// </summary>
    /// <param name="codeElement">The element to write internal usings from</param>
    /// <param name="writer">An instance of the language writer</param>
    /// <param name="parentNamespace">The code namespace of the code element</param>
    /// <returns>void</returns>
    public void WriteConditionalInternalImports(ClassDeclaration codeElement, LanguageWriter writer, CodeNamespace parentNameSpace)
    {
        var internalImportSymbolsAndPaths = codeElement.Usings
                                                        .Where(x => !x.IsExternal)
                                                        .Select(x => _relativeImportManager.GetRelativeImportPathForUsing(x, parentNameSpace))
                                                        .GroupBy(x => x.Item3)
                                                        .Where(x => !string.IsNullOrEmpty(x.Key))
                                                        .OrderBy(x => x.Key);
        if (internalImportSymbolsAndPaths.Any())
        {
            writer.WriteLine("if TYPE_CHECKING:");
            writer.IncreaseIndent();
            foreach (var codeUsing in internalImportSymbolsAndPaths)
                writer.WriteLine($"from {codeUsing.Key.ToSnakeCase()} import {codeUsing.Select(x => GetAliasedSymbol(x.Item1, x.Item2)).Distinct().OrderBy(x => x).Aggregate((x, y) => x + ", " + y)}");
            writer.DecreaseIndent();
            writer.WriteLine();

        }
    }
    /// <summary>
    /// Writes local imports for a given type.
    /// </summary>
    /// <param name="parentClass">The parent CodeClass of the given type</param>
    /// <param name="typeName">The name of the given type. Must be a valid using declaration name in the parentClass</param>
    /// <param name="writer">An instance of the language writer</param>
    /// <returns>void</returns>
    public void WriteDeferredImport(CodeClass parentClass, string typeName, LanguageWriter writer)
    {
        var parentNamespace = parentClass.GetImmediateParentOfType<CodeNamespace>();
        var internalImportSymbolsAndPaths = parentClass.Usings
                                                        .Where(x => !x.IsExternal)
                                                        .Where(x => string.Equals(x.Declaration?.Name, typeName))
                                                        .Select(x => _relativeImportManager.GetRelativeImportPathForUsing(x, parentNamespace))
                                                        .GroupBy(x => x.Item3)
                                                        .Where(x => !string.IsNullOrEmpty(x.Key))
                                                        .OrderBy(x => x.Key);
        WriteCodeUsings(internalImportSymbolsAndPaths, writer);
    }

    private static void WriteCodeUsings(IOrderedEnumerable<IGrouping<string, (string, string, string)>> importSymbolsAndPaths, LanguageWriter writer)
    {
        if (importSymbolsAndPaths.Any())
        {
            foreach (var codeUsing in importSymbolsAndPaths)
                writer.WriteLine($"from {codeUsing.Key.ToSnakeCase()} import {codeUsing.Select(x => GetAliasedSymbol(x.Item1, x.Item2)).Distinct().OrderBy(x => x).Aggregate((x, y) => x + ", " + y)}");
            writer.WriteLine();
        }
    }
    private static string GetAliasedSymbol(string symbol, string alias)
    {
        return string.IsNullOrEmpty(alias) ? symbol : $"{symbol} as {alias}";
    }
}
