using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                                                            .Select(x => (x.Name, x.Declaration?.Name));
            var internalImportSymbolsAndPaths = codeElement.Usings
                                                            .Where(x => !x.IsExternal)
                                                            .Select(x => _relativeImportManager.GetRelativeImportPathForUsing(x, parentNamespace));
            var importSymbolsAndPaths = externalImportSymbolsAndPaths.Union(internalImportSymbolsAndPaths)
                                                                    .GroupBy(x => x.Item2)
                                                                    .OrderBy(x => x.Key);
            foreach (var codeUsing in importSymbolsAndPaths)
                writer.WriteLine($"import {{{codeUsing.Select(x => x.Item1).Distinct().Aggregate((x,y) => x + ", " + y)}}} from '{codeUsing.Key}';");
            writer.WriteLine();
            var derivation = (codeElement.Inherits == null ? string.Empty : $" extends {codeElement.Inherits.Name.ToFirstCharacterUpperCase()}") +
                            (!codeElement.Implements.Any() ? string.Empty : $" implements {codeElement.Implements.Select(x => x.Name).Aggregate((x,y) => x + ", " + y)}");
            conventions.WriteShortDescription((codeElement.Parent as CodeClass).Description, writer);
            writer.WriteLine($"export class {codeElement.Name.ToFirstCharacterUpperCase()}{derivation} {{");
            writer.IncreaseIndent();
        }
    }
}
