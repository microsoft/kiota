using System;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Python {
    public class CodeClassDeclarationWriter : BaseElementWriter<ClassDeclaration, PythonConventionService>
    {
        private readonly PythonRelativeImportManager _relativeImportManager;
        public CodeClassDeclarationWriter(PythonConventionService conventionService, string clientNamespaceName) : base(conventionService){
            _relativeImportManager = new PythonRelativeImportManager(clientNamespaceName, '.');
        }
        public override void WriteCodeElement(ClassDeclaration codeElement, LanguageWriter writer)
        {
            if(codeElement == null) throw new ArgumentNullException(nameof(codeElement));
            if(writer == null) throw new ArgumentNullException(nameof(writer));
            var parentNamespace = codeElement.GetImmediateParentOfType<CodeNamespace>();
            AddExternalImports(codeElement, writer); // external imports before internal imports
            AddInternalImports(codeElement, writer, parentNamespace);
            
            var inheritSymbol = conventions.GetTypeString(codeElement.Inherits, codeElement);
            var abcClass = !codeElement.Implements.Any() ? string.Empty : $"{codeElement.Implements.Select(x => x.Name.ToFirstCharacterUpperCase()).Aggregate((x,y) => x + ", " + y)}";
            var derivation = inheritSymbol == null ? abcClass : $"{inheritSymbol}";
            if(codeElement.Parent?.Parent is CodeClass){
                writer.WriteLine($"@dataclass");
            }
            writer.WriteLine($"class {codeElement.Name.ToFirstCharacterUpperCase()}({derivation}):");
            writer.IncreaseIndent();
            conventions.WriteShortDescription((codeElement.Parent as CodeClass).Description, writer);
        }
        
        private static void AddExternalImports(ClassDeclaration codeElement, LanguageWriter writer) {
            var externalImportSymbolsAndPaths = codeElement.Usings
                                                            .Where(x => x.IsExternal)
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

        private void AddInternalImports(ClassDeclaration codeElement, LanguageWriter writer, CodeNamespace parentNameSpace) {
            var internalImportSymbolsAndPaths = codeElement.Usings
                                                            .Where(x => !x.IsExternal)
                                                            .Select(x => _relativeImportManager.GetRelativeImportPathForUsing(x, parentNameSpace))
                                                            .GroupBy(x => x.Item3)
                                                            .OrderBy(x => x.Key);
            if(internalImportSymbolsAndPaths.Any()){
                foreach (var codeUsing in internalImportSymbolsAndPaths)
                    if (!string.IsNullOrWhiteSpace(codeUsing.Key))
                    {
                        writer.WriteLine($"from {codeUsing.Key.ToSnakeCase()} import {codeUsing.Select(x => GetAliasedSymbol(x.Item1, x.Item2)).Distinct().OrderBy(x => x).Aggregate((x,y) => x + ", " + y)}");
                    }
                writer.WriteLine();
            }
        }
        private static string GetAliasedSymbol(string symbol, string alias) {
            return string.IsNullOrEmpty(alias) ? symbol : $"{symbol} as {alias}";
        }
    }
}
