using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.TypeScript {
    public class CodeClassDeclarationWriter : BaseElementWriter<CodeClass.Declaration, TypeScriptConventionService>
    {
        private readonly RelativeImportManager _relativeImportManager;
        public CodeClassDeclarationWriter(TypeScriptConventionService conventionService, string clientNamespaceName) : base(conventionService){
            _relativeImportManager = new RelativeImportManager(clientNamespaceName, '.');
        }
        public override void WriteCodeElement(CodeClass.Declaration codeElement, LanguageWriter writer)
        {
            var parentNamespace = codeElement.GetImmediateParentOfType<CodeNamespace>();
            var externalImportSymbolsAndPaths = codeElement.Usings
                                                            .Where(x => x.IsExternal)
                                                            .Select(x => (x.Name, string.Empty, x.Declaration?.Name));
            var internalImportSymbolsAndPaths = codeElement.Usings
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
            var inheritSymbol = conventions.GetTypeString(codeElement.Inherits, codeElement);
            var derivation = (inheritSymbol == null ? string.Empty : $" extends {inheritSymbol}") +
                            (!codeElement.Implements.Any() ? string.Empty : $" implements {codeElement.Implements.Select(x => x.Name).Aggregate((x,y) => x + ", " + y)}");
            conventions.WriteShortDescription((codeElement.Parent as CodeClass).Description, writer);
            writer.WriteLine($"export class {codeElement.Name.ToFirstCharacterUpperCase()}{derivation} {{");
            writer.IncreaseIndent();
        }
        private static string GetAliasedSymbol(string symbol, string alias) {
            return string.IsNullOrEmpty(alias) ? symbol : $"{symbol} as {alias}";
        }
    }
}
