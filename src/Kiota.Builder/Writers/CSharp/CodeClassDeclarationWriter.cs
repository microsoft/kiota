using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.CSharp {
    public class CodeClassDeclarationWriter : BaseCSharpElementWriter<CodeClass.Declaration>
    {
        public CodeClassDeclarationWriter(CSharpConventionService conventionService): base(conventionService) { }
        public override void WriteCodeElement(CodeClass.Declaration codeElement, LanguageWriter writer)
        {
            foreach (var codeUsing in codeElement.Usings.Where(x => !string.IsNullOrEmpty(x.Name) && x.Declaration == null)
                                                .Select(x => x.Name)
                                                .Distinct()
                                                .OrderBy(x => x))
                writer.WriteLine($"using {codeUsing};");
            foreach (var codeUsing in codeElement.Usings.Where(x => !string.IsNullOrEmpty(x.Name) && x.Declaration != null)
                                                .Select(x => x.Name)
                                                .Distinct()
                                                .OrderBy(x => x))
                writer.WriteLine($"using {codeUsing.Split('.').Select(x => x.ToFirstCharacterUpperCase()).Aggregate((x,y) => x + "." + y)};");
            if(codeElement?.Parent?.Parent is CodeNamespace) {
                writer.WriteLine($"namespace {codeElement.Parent.Parent.Name} {{");
                writer.IncreaseIndent();
            }

            var derivedTypes = new List<string>{codeElement.Inherits?.Name}
                                            .Union(codeElement.Implements.Select(x => x.Name))
                                            .Where(x => x != null);
            var derivation = derivedTypes.Any() ? ": " +  derivedTypes.Select(x => x.ToFirstCharacterUpperCase()).Aggregate((x, y) => $"{x}, {y}") + " " : string.Empty;
            if(codeElement.Parent is CodeClass parentClass)
                conventions.WriteShortDescription(parentClass.Description, writer);
            writer.WriteLine($"public class {codeElement.Name.ToFirstCharacterUpperCase()} {derivation}{{");
            writer.IncreaseIndent();
        }
    }
}
