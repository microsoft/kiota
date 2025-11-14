using System;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Java;

public class CodeClassDeclarationWriter : BaseElementWriter<ClassDeclaration, JavaConventionService>
{
    public CodeClassDeclarationWriter(JavaConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(ClassDeclaration codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.Parent?.Parent is CodeNamespace ns)
        {
            writer.WriteLine($"package {ns.Name};");
            writer.WriteLine();
            codeElement.Usings
                .Union(codeElement.Parent is CodeClass cClass ? cClass.InnerClasses.SelectMany(static x => x.Usings) : Enumerable.Empty<CodeUsing>())
                .Where(static x => x.Declaration != null)
                .Where(x => x.Declaration!.IsExternal || !x.Declaration.Name.Equals(codeElement.Name, StringComparison.OrdinalIgnoreCase)) // needed for circular requests patterns like message folder
                .Select(static x => x.Declaration!.IsExternal ?
                                    $"import {x.Declaration.Name}.{x.Name};" :
                                    $"import {x.Name}.{x.Declaration.Name};")
                .Distinct()
                .GroupBy(static x => x.Split('.').Last(), StringComparer.OrdinalIgnoreCase)
                .Select(static x => x.First()) // we don't want to import the same symbol twice
                .OrderBy(static x => x)
                .ToList()
                .ForEach(x => writer.WriteLine(x));
        }
        var derivation = (codeElement.Inherits == null ? string.Empty : $" extends {codeElement.Inherits.Name}") +
                        (!codeElement.Implements.Any() ? string.Empty : $" implements {codeElement.Implements.Select(x => x.Name).Aggregate((x, y) => x + ", " + y)}");
        if (codeElement.Parent is CodeClass parentClass)
            conventions.WriteLongDescription(parentClass, writer);
        var innerClassStatic = codeElement.Parent is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model) && currentClass.Parent is CodeClass ? "static " : string.Empty; //https://stackoverflow.com/questions/47541459/no-enclosing-instance-is-accessible-must-qualify-the-allocation-with-an-enclosi
        writer.WriteLine(JavaConventionService.AutoGenerationHeader);
        writer.WriteLine($"public {innerClassStatic}class {codeElement.Name}{derivation} {{");
        writer.IncreaseIndent();
    }
}
