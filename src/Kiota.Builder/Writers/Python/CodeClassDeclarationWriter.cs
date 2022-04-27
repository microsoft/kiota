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
            AddStandardImports(writer);
            var parentNamespace = codeElement.GetImmediateParentOfType<CodeNamespace>();
            var externalImportSymbolsAndPaths = codeElement.Usings
                                                            .Where(x => x.IsExternal)
                                                            .Select(x => (x.Name, string.Empty, x.Declaration?.Name))
                                                            .GroupBy(x => x.Item3)
                                                            .OrderBy(x => x.Key);
            foreach (var codeUsing in externalImportSymbolsAndPaths) // external imports before internal imports
                if (!string.IsNullOrWhiteSpace(codeUsing.Key))
                {
                    writer.WriteLine($"from {codeUsing.Key.ToSnakeCase()} import {codeUsing.Select(x => GetAliasedSymbol(x.Item1, x.Item2)).Distinct().OrderBy(x => x).Aggregate((x,y) => x + ", " + y)}");
                }
            writer.WriteLine();
            var internalImportSymbolsAndPaths = codeElement.Usings
                                                            .Where(x => !x.IsExternal)
                                                            .Select(x => _relativeImportManager.GetRelativeImportPathForUsing(x, parentNamespace))
                                                            .GroupBy(x => x.Item3)
                                                            .OrderBy(x => x.Key);

            foreach (var codeUsing in internalImportSymbolsAndPaths)
                if (!string.IsNullOrWhiteSpace(codeUsing.Key))
                {
                    writer.WriteLine($"from {codeUsing.Key.ToSnakeCase()} import {codeUsing.Select(x => GetAliasedSymbol(x.Item1, x.Item2)).Distinct().OrderBy(x => x).Aggregate((x,y) => x + ", " + y)}");
                }
            writer.WriteLine();
            var inheritSymbol = conventions.GetTypeString(codeElement.Inherits, codeElement);
            var abcClass = !codeElement.Implements.Any() ? string.Empty : $"{codeElement.Implements.Select(x => x.Name).Aggregate((x,y) => x + ", " + y)}";
            var derivation = inheritSymbol == null ? abcClass : $"{inheritSymbol.ToFirstCharacterUpperCase()}";
            writer.WriteLine($"class {codeElement.Name.ToFirstCharacterUpperCase()}({derivation}):");
            writer.IncreaseIndent();
            conventions.WriteShortDescription((codeElement.Parent as CodeClass).Description, writer);
            
        }
        private static string GetAliasedSymbol(string symbol, string alias) {
            return string.IsNullOrEmpty(alias) ? symbol : $"{symbol} as {alias}";
        }

        private static void AddStandardImports(LanguageWriter writer) {
            writer.WriteLine("from __future__ import annotations");
            writer.WriteLine("from typing import Any, Callable, Dict, List, Optional, Union");
            writer.WriteLine();
        }
    }
}
