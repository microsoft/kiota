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
            AddStandardImports(writer);
            var externalImportSymbolsAndPaths = codeElement.Usings
                                                            .Where(x => x.IsExternal)
                                                            .Select(x => (x.Name, string.Empty, x.Declaration?.Name))
                                                            .GroupBy(x => x.Item3)
                                                            .OrderBy(x => x.Key);
            if(externalImportSymbolsAndPaths.Any()){
                foreach (var codeUsing in externalImportSymbolsAndPaths) // external imports before internal imports
                if (!string.IsNullOrWhiteSpace(codeUsing.Key))
                {
                    if (codeUsing.Key == "-")
                        writer.WriteLine($"import {codeUsing.Select(x => GetAliasedSymbol(x.Item1, x.Item2)).Distinct().OrderBy(x => x).Aggregate((x,y) => x + ", " + y)}");
                    else
                        writer.WriteLine($"from {codeUsing.Key.ToSnakeCase()} import {codeUsing.Select(x => GetAliasedSymbol(x.Item1, x.Item2)).Distinct().OrderBy(x => x).Aggregate((x,y) => x + ", " + y)}");
                }
                writer.WriteLine();
            }
            var internalImportSymbolsAndPaths = codeElement.Usings
                                                            .Where(x => !x.IsExternal)
                                                            .Select(x => _relativeImportManager.GetRelativeImportPathForUsing(x, parentNamespace))
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
        private static string GetAliasedSymbol(string symbol, string alias) {
            return string.IsNullOrEmpty(alias) ? symbol : $"{symbol} as {alias}";
        }

        private static void AddStandardImports(LanguageWriter writer) {
            if(string.IsNullOrEmpty(writer.GetIndent())){  // Don't add for inner classes
                writer.WriteLine("from __future__ import annotations");
                writer.WriteLine("from typing import Any, Callable, Dict, List, Optional, Union");
            }            
        }
    }
}
