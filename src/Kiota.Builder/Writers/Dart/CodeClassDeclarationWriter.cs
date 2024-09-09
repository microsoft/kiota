using System;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Go;

namespace Kiota.Builder.Writers.Dart;
public class CodeClassDeclarationWriter : BaseElementWriter<ClassDeclaration, DartConventionService>
{
    public CodeClassDeclarationWriter(DartConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(ClassDeclaration codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);

        if (codeElement.Parent is not CodeClass parentClass)
            throw new InvalidOperationException($"The provided code element {codeElement.Name} doesn't have a parent of type {nameof(CodeClass)}");

        if (codeElement.Parent?.Parent is CodeNamespace)
        {
            codeElement.Usings
                    .Where(x => (x.Declaration?.IsExternal ?? true) || !x.Declaration.Name.Equals(codeElement.Name, StringComparison.OrdinalIgnoreCase)) // needed for circular requests patterns like message folder
                    .Select(x => x.Declaration?.IsExternal ?? false ?
                                    $"import 'package:{x.Declaration.Name}.dart';" :
                                    getImportStatement(x, codeElement))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(static x => x, StringComparer.Ordinal)
                    .ToList()
                    .ForEach(x => writer.WriteLine(x));
            writer.WriteLine();
        }

        var derivation = codeElement.Inherits == null ? string.Empty : $" extends {codeElement.Inherits.Name}";
        var implements = !codeElement.Implements.Any() ? string.Empty : $" implements {codeElement.Implements.Select(x => x.Name).Aggregate((x, y) => x + ", " + y)}";

        conventions.WriteLongDescription(parentClass, writer);
        conventions.WriteDeprecationAttribute(parentClass, writer);
        writer.StartBlock($"class {codeElement.Name.ToFirstCharacterUpperCase()}{derivation}{implements} {{");
    }

    private static string getImportStatement(CodeUsing x, ClassDeclaration codeElement)
    {
        return codeElement.GetNamespaceDepth() == 1 ?
             $"import './{x.Name.Split('.')[1]}/{x.Declaration!.Name.ToSnakeCase()}.dart';"
             : $"import '../{x.Name.Split('.')[1]}/{x.Declaration!.Name.ToSnakeCase()}.dart';";
    }
}
