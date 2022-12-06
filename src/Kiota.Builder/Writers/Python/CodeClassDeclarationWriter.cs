using System;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Python;
public class CodeClassDeclarationWriter : BaseElementWriter<ClassDeclaration, PythonConventionService>
{

    public CodeClassDeclarationWriter(PythonConventionService conventionService) : base(conventionService){
    }
    public override void WriteCodeElement(ClassDeclaration codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        if(writer == null) throw new ArgumentNullException(nameof(writer));
        WriteExternalImports(codeElement, writer); // external imports before internal imports
        WriteInternalImports(codeElement, writer);
        
        var inheritSymbol = conventions.GetTypeString(codeElement.Inherits, codeElement);
        var abcClass = !codeElement.Implements.Any() ? string.Empty : $"{codeElement.Implements.Select(x => x.Name.ToFirstCharacterUpperCase()).Aggregate((x,y) => x + ", " + y)}";
        var derivation = inheritSymbol == null ? abcClass : $"{inheritSymbol}";
        if(codeElement.Parent?.Parent is CodeClass){
            writer.WriteLine("@dataclass");
        }
        writer.WriteLine($"class {codeElement.Name.ToFirstCharacterUpperCase()}({derivation}):");
        writer.IncreaseIndent();
        conventions.WriteShortDescription((codeElement.Parent as CodeClass)?.Description, writer);
    }
    
    private static void WriteExternalImports(ClassDeclaration codeElement, LanguageWriter writer) {
        var externalImportSymbolsAndPaths = codeElement.Usings
                                                        .Where(static x => x.IsExternal)
                                                        .Select(x => (x.Name, string.Empty, x.Declaration?.Name))
                                                        .GroupBy(x => x.Item3)
                                                        .OrderBy(x => x.Key);
        if(externalImportSymbolsAndPaths.Any()){
            foreach (var codeUsing in externalImportSymbolsAndPaths)
                if (!string.IsNullOrWhiteSpace(codeUsing.Key))
                {
                    if (codeUsing.Key == "-")
                        writer.WriteLine($"import {codeUsing.Select(x => GetAliasedSymbol(x.Item1, x.Item2)).Distinct().OrderBy(x => x).Aggregate((x,y) => x + ", " + y)}");
                    else
                        writer.WriteLine($"from {codeUsing.Key.ToSnakeCase()} import {codeUsing.Select(x => GetAliasedSymbol(x.Item1, x.Item2)).Distinct().OrderBy(x => x).Aggregate((x,y) => x + ", " + y)}");
                }
            writer.WriteLine();
        }
    }

    private static void WriteInternalImports(ClassDeclaration codeElement, LanguageWriter writer) {
        var internalImportSymbolsAndPaths = codeElement.Usings
                                                        .Where(x => !x.IsExternal)
                                                        .Select(x => GetImportPathForUsing(x))
                                                        .GroupBy(x => x.Item3)
                                                        .Where(x => !string.IsNullOrEmpty(x.Key))
                                                        .OrderBy(x => x.Key);
        if(internalImportSymbolsAndPaths.Any()){
            foreach (var codeUsing in internalImportSymbolsAndPaths)
                foreach(var symbol in codeUsing.Select(x => GetAliasedSymbol(x.Item1, x.Item2)).Distinct().OrderBy(x => x))
                    writer.WriteLine($"{symbol} = lazy_import('{codeUsing.Key.ToSnakeCase().Replace("._", ".")}.{symbol}')"); 
            writer.WriteLine();
        }
    }
    private static string GetAliasedSymbol(string symbol, string alias) {
        return string.IsNullOrEmpty(alias) ? symbol : $"{symbol} as {alias}";
    }

    /// <summary>
    /// Returns the import path for the given using and import context namespace.
    /// </summary>
    /// <param name="codeUsing">The using to import into the current namespace context</param>
    /// <returns>The import symbol, it's alias if any and the import path</returns>
    private static (string, string, string) GetImportPathForUsing(CodeUsing codeUsing)
    {         
        var typeDef = codeUsing.Declaration?.TypeDefinition;
        if (typeDef == null)
            return (codeUsing.Name, codeUsing.Alias, ""); // it's relative to the folder, with no declaration or type definition (default failsafe)
        
        var importSymbol = typeDef.Name.ToSnakeCase();
        
        var importPath = typeDef.GetImmediateParentOfType<CodeNamespace>().Name;
        return (importSymbol, codeUsing.Alias, importPath);
    }
}
