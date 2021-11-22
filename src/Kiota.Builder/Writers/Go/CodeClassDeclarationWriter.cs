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
                writer.WriteLine($"package {ns.Name.GetLastNamespaceSegment().Replace("-", string.Empty)}");
            var importSegments = codeElement
                                .Usings
                                .Where(x => !x.Declaration.IsExternal)
                                .Select(x => x.GetInternalNamespaceImport())
                                .Select(x => new Tuple<string, string>(x.GetNamespaceImportSymbol(), x))
                                .Distinct()
                                .Union(codeElement
                                    .Usings
                                    .Where(x => x.Declaration.IsExternal)
                                    .Select(x => new Tuple<string, string>(x.Name.StartsWith("*") ? x.Name[1..] : x.Declaration.Name.GetNamespaceImportSymbol(), x.Declaration.Name))
                                    .Distinct())
                                .OrderBy(x => x.Item2.Count(y => y == '/'))
                                .ThenBy(x => x)
                                .ToList();
            if(importSegments.Any()) {
                writer.WriteLines(string.Empty, "import (");
                writer.IncreaseIndent();
                importSegments.ForEach(x => writer.WriteLine($"{x.Item1} \"{x.Item2}\""));
                writer.DecreaseIndent();
                writer.WriteLines(")", string.Empty);
            }
            conventions.WriteShortDescription($"{codeElement.Parent.Name} {(codeElement.Parent as CodeClass).Description.ToFirstCharacterLowerCase()}", writer);
            writer.WriteLine($"type {codeElement.Name.ToFirstCharacterUpperCase()} struct {{");
            writer.IncreaseIndent();
            if(codeElement.Inherits?.AllTypes?.Any() ?? false) {
                var parentTypeName = conventions.GetTypeString(codeElement.Inherits.AllTypes.First(), codeElement.Parent.Parent, true, false);
                writer.WriteLine($"{parentTypeName}");
            }
        }
    }
}
