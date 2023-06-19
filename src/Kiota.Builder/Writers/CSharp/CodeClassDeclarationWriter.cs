using System;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.CSharp;
public class CodeClassDeclarationWriter : BaseElementWriter<ClassDeclaration, CSharpConventionService>
{
    public CodeClassDeclarationWriter(CSharpConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(ClassDeclaration codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.Parent?.Parent is CodeNamespace)
        {
            codeElement.Usings
                    .Where(x => (x.Declaration?.IsExternal ?? true) || !x.Declaration.Name.Equals(codeElement.Name, StringComparison.OrdinalIgnoreCase)) // needed for circular requests patterns like message folder
                    .Select(static x => x.Declaration?.IsExternal ?? false ?
                                    $"using {x.Declaration.Name.NormalizeNameSpaceName(".")};" :
                                    $"using {x.Name.NormalizeNameSpaceName(".")};")
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(static x => x, StringComparer.Ordinal)
                    .ToList()
                    .ForEach(x => writer.WriteLine(x));
            writer.StartBlock($"namespace {codeElement.Parent.Parent.Name} {{");
        }

        var derivedTypes = new string?[] { codeElement.Inherits?.Name }
                                        .Union(codeElement.Implements.Select(static x => x.Name))
                                        .Where(static x => x != null)
                                        .Select(static x => x.ToFirstCharacterUpperCase())
                                        .ToArray();
        var derivation = derivedTypes.Any() ? ": " + derivedTypes.Aggregate(static (x, y) => $"{x}, {y}") + " " : string.Empty;
        if (codeElement.Parent is CodeClass parentClass)
        {
            conventions.WriteLongDescription(parentClass.Documentation, writer);
            var deprecationMessage = conventions.GetDeprecationInformation(parentClass);
            if (!string.IsNullOrEmpty(deprecationMessage))
                writer.WriteLine(deprecationMessage);
        }
        writer.WriteLine($"public class {codeElement.Name.ToFirstCharacterUpperCase()} {derivation}{{");
        writer.IncreaseIndent();
    }
}
