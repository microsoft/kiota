using System;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Crystal;
public class CodeClassDeclarationWriter : BaseElementWriter<ClassDeclaration, CrystalConventionService>
{
    public static string AutoGenerationHeader => "# This file was auto-generated";
    public CodeClassDeclarationWriter(CrystalConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(ClassDeclaration codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.Parent is not CodeClass parentClass) throw new InvalidOperationException($"The provided code element {codeElement.Name} doesn't have a parent of type {nameof(CodeClass)}");

        if (codeElement.Parent?.Parent is CodeNamespace)
        {
            writer.WriteLine(AutoGenerationHeader);
            codeElement.Usings
                    .Where(x => (x.Declaration?.IsExternal ?? true) || !x.Declaration.Name.Equals(codeElement.Name, StringComparison.OrdinalIgnoreCase))
                    .Select(static x => x.Declaration?.IsExternal ?? false ?
                                    $"require \"{x.Declaration.Name.ToSnakeCase()}\"" :
                                    $"require \"{x.Name.ToSnakeCase()}\"")
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(static x => x, StringComparer.Ordinal)
                    .ToList()
                    .ForEach(x => writer.WriteLine(x));
            writer.WriteLine($"module {codeElement.Parent.Parent.Name.ToFirstCharacterUpperCase()}");
            writer.IncreaseIndent();
        }

        var derivedTypes = (codeElement.Inherits is null ? Enumerable.Empty<string?>() : new string?[] { conventions.GetTypeString(codeElement.Inherits, parentClass) })
                                        .Union(codeElement.Implements.Select(static x => x.Name))
                                        .OfType<string>()
                                        .ToArray();
        var derivation = derivedTypes.Length != 0 ? "< " + derivedTypes.Aggregate(static (x, y) => $"{x}, {y}") : string.Empty;
        bool hasDescription = conventions.WriteLongDescription(parentClass, writer);
        writer.WriteLine($"class {codeElement.Name.ToFirstCharacterUpperCase()} {derivation}");
        writer.IncreaseIndent();

        // Include modules
        if (codeElement.Implements != null)
        {
            foreach (var implementedInterface in codeElement.Implements)
            {
                writer.WriteLine($"include {implementedInterface.Name}");
            }
        }

        // Write abstract methods if any
        foreach (var method in parentClass.Methods.Where(m => m.IsAbstract()))
        {
            writer.WriteLine($"abstract def {method.Name.ToSnakeCase()}({string.Join(", ", method.Parameters.Select(p => $"{conventions.GetParameterSignature(p, method)} : {conventions.GetTypeString(p.Type, p)}"))}) : {conventions.GetTypeString(method.ReturnType, method)}");
        }
    }
}
