using System;
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
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        var externalImportSymbolsAndPaths = codeElement.Usings
                                                        .Where(static x => x.IsExternal)
                                                        .Select(static x => (x.Name, string.Empty, x.Declaration?.Name))
                                                        .GroupBy(static x => x.Item3)
                                                        .OrderBy(static x => x.Key)
                                                        .ToArray();
        if (externalImportSymbolsAndPaths.Length != 0)
        {
            foreach (var codeUsing in externalImportSymbolsAndPaths)
                if (!string.IsNullOrWhiteSpace(codeUsing.Key))
                {
                    if ("-".Equals(codeUsing.Key, StringComparison.OrdinalIgnoreCase))
                        writer.WriteLine($"import {codeUsing.Select(x => GetAliasedSymbol(x.Item1, x.Item2)).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).Aggregate(static (x, y) => x + ", " + y)}");
                    else
                        writer.WriteLine($"from {codeUsing.Key} import {codeUsing.Select(x => GetAliasedSymbol(x.Item1, x.Item2)).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).Aggregate(static (x, y) => x + ", " + y)}");
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
        ArgumentNullException.ThrowIfNull(parentClass);
        ArgumentNullException.ThrowIfNull(writer);
        var parentNameSpace = parentClass.GetImmediateParentOfType<CodeNamespace>();
        var internalErrorMappingImportSymbolsAndPaths = parentClass.Usings
                                                        .Where(static x => !x.IsExternal)
                                                        .Where(static x => x.Declaration?.TypeDefinition is CodeClass codeClass && codeClass.IsErrorDefinition)
                                                        .Select(x => _relativeImportManager.GetRelativeImportPathForUsing(x, parentNameSpace))
                                                        .GroupBy(static x => x.Item3)
                                                        .Where(static x => !string.IsNullOrEmpty(x.Key))
                                                        .OrderBy(static x => x.Key, StringComparer.OrdinalIgnoreCase)
                                                        .ToArray();
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
        ArgumentNullException.ThrowIfNull(parentClass);
        ArgumentNullException.ThrowIfNull(writer);
        var parentNameSpace = parentClass.GetImmediateParentOfType<CodeNamespace>();
        var internalImportSymbolsAndPaths = parentClass.Usings
                                                        .Where(static x => !x.IsExternal)
                                                        .Select(x => _relativeImportManager.GetRelativeImportPathForUsing(x, parentNameSpace))
                                                        .GroupBy(static x => x.Item3)
                                                        .Where(static x => !string.IsNullOrEmpty(x.Key))
                                                        .OrderBy(static x => x.Key, StringComparer.OrdinalIgnoreCase)
                                                        .ToArray();
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
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        var internalImportSymbolsAndPaths = codeElement.Usings
                                                        .Where(static x => !x.IsExternal)
                                                        .Select(x => _relativeImportManager.GetRelativeImportPathForUsing(x, parentNameSpace))
                                                        .GroupBy(static x => x.Item3)
                                                        .Where(static x => !string.IsNullOrEmpty(x.Key))
                                                        .OrderBy(static x => x.Key, StringComparer.OrdinalIgnoreCase)
                                                        .ToArray();
        if (internalImportSymbolsAndPaths.Length != 0)
        {
            writer.WriteLine("if TYPE_CHECKING:");
            writer.IncreaseIndent();
            foreach (var codeUsing in internalImportSymbolsAndPaths)
                writer.WriteLine($"from {codeUsing.Key} import {codeUsing.Select(x => GetAliasedSymbol(x.Item1, x.Item2)).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).Aggregate(static (x, y) => x + ", " + y)}");
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
        ArgumentNullException.ThrowIfNull(parentClass);
        ArgumentNullException.ThrowIfNull(writer);
        var parentNamespace = parentClass.GetImmediateParentOfType<CodeNamespace>();
        var internalImportSymbolsAndPaths = parentClass.Usings
                                                        .Where(static x => !x.IsExternal)
                                                        .Where(x => typeName.Equals(x.Declaration?.Name, StringComparison.OrdinalIgnoreCase))
                                                        .Select(x => _relativeImportManager.GetRelativeImportPathForUsing(x, parentNamespace))
                                                        .GroupBy(static x => x.Item3)
                                                        .Where(static x => !string.IsNullOrEmpty(x.Key))
                                                        .OrderBy(static x => x.Key, StringComparer.OrdinalIgnoreCase)
                                                        .ToArray();
        WriteCodeUsings(internalImportSymbolsAndPaths, writer);
    }

    private static void WriteCodeUsings(IGrouping<string, (string, string, string)>[] importSymbolsAndPaths, LanguageWriter writer)
    {
        if (importSymbolsAndPaths.Length != 0)
        {
            foreach (var codeUsing in importSymbolsAndPaths)
                writer.WriteLine($"from {codeUsing.Key} import {codeUsing.Select(x => GetAliasedSymbol(x.Item1, x.Item2)).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).Aggregate(static (x, y) => x + ", " + y)}");
            writer.WriteLine();
        }
    }
    private static string GetAliasedSymbol(string symbol, string alias)
    {
        return string.IsNullOrEmpty(alias) ? symbol : $"{symbol} as {alias}";
    }
}
