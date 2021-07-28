using System;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Go {
    public class CodeClassDeclarationWriter : BaseElementWriter<CodeClass.Declaration, GoConventionService>
    {
        public CodeClassDeclarationWriter(GoConventionService conventionService) : base(conventionService) {}

        public override void WriteCodeElement(CodeClass.Declaration codeElement, LanguageWriter writer)
        {
            var currentNamespaceSlashCount = 0;
            if(codeElement?.Parent?.Parent is CodeNamespace ns) {
                writer.WriteLine($"package {ns.Name.GetLastNamespaceSegment().ToLowerInvariant()}");
                currentNamespaceSlashCount = ns.GetInternalNamespaceImport().Count(x => x == '/');
            }
            var importSegments = codeElement
                                .Usings
                                .Where(x => !x.Declaration.IsExternal)
                                .Select(i => i.GetInternalNamespaceImport())
                                .Where(x => x.Count(y => y == '/') != currentNamespaceSlashCount) // only the submoules or parent ones
                                .Distinct()
                                .ToList();
            importSegments.AddRange(codeElement
                                    .Usings
                                    .Where(x => x.Declaration.IsExternal)
                                    .Where(x => !x.Declaration.Name.EndsWith("serialization")) //TODO remove when we have code method writer implemented
                                    .Select(i => i.Declaration.Name)
                                    .Distinct());
            if(importSegments.Any()) {
                writer.WriteLines(string.Empty, "import (");
                writer.IncreaseIndent();
                importSegments.ForEach(x => writer.WriteLine($"{x.GetNamespaceImportSymbol()} \"{x}\""));
                writer.DecreaseIndent();
                writer.WriteLines(")", string.Empty);
            }
            writer.WriteLine($"type {codeElement.Name.ToFirstCharacterUpperCase()} struct {{");
            writer.IncreaseIndent();
        }
    }
}
