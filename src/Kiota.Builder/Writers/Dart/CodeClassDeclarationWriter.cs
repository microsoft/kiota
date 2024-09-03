using System;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

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
                    .Select(static x => x.Declaration?.IsExternal ?? false ?
                                    $"import 'package:{x.Declaration.Name}.dart';" :
                                    $"import 'package:{x.Name}.dart';")
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(static x => x, StringComparer.Ordinal)
                    .ToList()
                    .ForEach(x => writer.WriteLine(x));
            writer.WriteLine();
        }

        var derivedTypes = (codeElement.Inherits is null ? Enumerable.Empty<string?>() : [conventions.GetTypeString(codeElement.Inherits, parentClass)])
                                        .Union(codeElement.Implements.Select(static x => x.Name))
                                        .OfType<string>()
                                        .Select(static x => x.ToFirstCharacterUpperCase())
                                        .ToArray();
        var derivation = derivedTypes.Length != 0 ? "extends " + derivedTypes.Aggregate(static (x, y) => $"{x}, {y}") + " " : string.Empty;
        conventions.WriteLongDescription(parentClass, writer);
        conventions.WriteDeprecationAttribute(parentClass, writer);
        writer.StartBlock($"class {codeElement.Name.ToFirstCharacterUpperCase()} {derivation}{{");
    }
}
