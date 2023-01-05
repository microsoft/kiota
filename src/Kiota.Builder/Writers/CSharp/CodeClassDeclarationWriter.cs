using System;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.CSharp;
public class CodeClassDeclarationWriter : BaseElementWriter<ClassDeclaration, CSharpConventionService>
{
    public CodeClassDeclarationWriter(CSharpConventionService conventionService): base(conventionService) { }
    public override void WriteCodeElement(ClassDeclaration codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if(codeElement.Parent?.Parent is CodeNamespace) {
            codeElement.Usings
                    .Where(x => (x.Declaration?.IsExternal ?? true) || !x.Declaration.Name.Equals(codeElement.Name, StringComparison.OrdinalIgnoreCase)) // needed for circular requests patterns like message folder
                    .Select(static x => x.Declaration?.IsExternal ?? false ?
                                    $"using {x.Declaration.Name.NormalizeNameSpaceName(".")};" :
                                    $"using {x.Name.NormalizeNameSpaceName(".")};")
                    .Distinct()
                    .OrderBy(static x => x)
                    .ToList()
                    .ForEach(x => writer.WriteLine(x));
            writer.StartBlock($"namespace {codeElement.Parent.Parent.Name} {{");
        }

        var derivedTypes = new List<string>{codeElement.Inherits?.Name}
                                        .Union(codeElement.Implements.Select(static x => x.Name))
                                        .Where(static x => x != null);
        var derivation = derivedTypes.Any() ? ": " +  derivedTypes.Select(static x => x.ToFirstCharacterUpperCase()).Aggregate(static (x, y) => $"{x}, {y}") + " " : string.Empty;
        if(codeElement.Parent is CodeClass parentClass)
            conventions.WriteLongDescription(parentClass.Documentation, writer);
        writer.WriteLine($"public class {codeElement.Name.ToFirstCharacterUpperCase()} {derivation}{{");
        writer.IncreaseIndent();
    }
}
