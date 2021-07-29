using System;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Go {
    public class CodeClassDeclarationWriter : BaseElementWriter<CodeClass.Declaration, GoConventionService>
    {
        public CodeClassDeclarationWriter(GoConventionService conventionService) : base(conventionService) {}

        public override void WriteCodeElement(CodeClass.Declaration codeElement, LanguageWriter writer)
        {
            if(codeElement?.Parent?.Parent is CodeNamespace ns)
                writer.WriteLine($"package {ns.Name.GetLastNamespaceSegment()}");
            var importSegments = codeElement
                                .Usings
                                .Where(x => !x.Declaration.IsExternal)
                                .Select(i => i.GetInternalNamespaceImport())
                                .Distinct()
                                .Union(codeElement
                                    .Usings
                                    .Where(x => x.Declaration.IsExternal)
                                    .Where(x => !x.Declaration.Name.EndsWith("serialization")) //TODO remove when we have code method writer implemented
                                    .Select(i => i.Declaration.Name)
                                    .Distinct())
                                .OrderBy(x => x.Count(y => y == '/'))
                                .ThenBy(x => x)
                                .ToList();
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
