using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.CSharp;

namespace Kiota.Builder.Writers.Shell
{
    class ShellCodeClassDeclarationWriter : CodeClassDeclarationWriter
    {
        public ShellCodeClassDeclarationWriter(CSharpConventionService conventionService) : base(conventionService)
        {
        }

        public override void WriteCodeElement(CodeClass.Declaration codeElement, LanguageWriter writer)
        {
            codeElement.Usings
                    .Where(x => (x.Declaration?.IsExternal ?? true) || !x.Declaration.Name.Equals(codeElement.Name, StringComparison.OrdinalIgnoreCase)) // needed for circular requests patterns like message folder
                    .Select(x => x.Declaration?.IsExternal ?? false ?
                                     $"using {x.Declaration.Name.NormalizeNameSpaceName(".")};" :
                                     $"using {x.Name.NormalizeNameSpaceName(".")};")
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList()
                    .ForEach(x => writer.WriteLine(x));
            if (codeElement?.Parent?.Parent is CodeNamespace)
            {
                writer.WriteLine($"namespace {codeElement.Parent.Parent.Name} {{");
                writer.IncreaseIndent();
            }
            var derivedTypes = new List<string> { codeElement.Inherits?.Name }
                                            .Union(codeElement.Implements.Select(x => x.Name))
                                            .Where(x => x != null);
            var derivation = derivedTypes.Any() ? ": " + derivedTypes.Select(x => x.ToFirstCharacterUpperCase()).Aggregate((x, y) => $"{x}, {y}") + " " : string.Empty;
            if (codeElement.Parent is CodeClass parentClass)
                conventions.WriteShortDescription(parentClass.Description, writer);
            var staticModifier = codeElement.IsStatic ? "static " : string.Empty;
            writer.WriteLine($"public {staticModifier}class {codeElement.Name.ToFirstCharacterUpperCase()} {derivation}{{");
            writer.IncreaseIndent();
        }
    }
}
