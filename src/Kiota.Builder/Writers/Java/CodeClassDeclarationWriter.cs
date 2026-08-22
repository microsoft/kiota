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
        if (codeElement.Parent is not CodeClass parentClass) throw new InvalidOperationException($"The provided code element {codeElement.Name} doesn't have a parent of type {nameof(CodeClass)}");
        var typeParameters = parentClass.TypeParameters.ToArray();
        var typeParametersDeclaration = typeParameters.Length != 0 ? $"<{string.Join(", ", typeParameters.Select(static x => $"{x.Name} extends Parsable"))}>" : string.Empty;
        var inherits = codeElement.Inherits is null ? string.Empty :
            $"{codeElement.Inherits.Name}{GetGenericArgumentsString(codeElement.Inherits, parentClass)}";
        var derivation = (string.IsNullOrEmpty(inherits) ? string.Empty : $" extends {inherits}") +
                        (!codeElement.Implements.Any() ? string.Empty : $" implements {codeElement.Implements.Select(x => x.Name).Aggregate((x, y) => x + ", " + y)}");
        conventions.WriteLongDescription(parentClass, writer);
        var innerClassStatic = parentClass.IsOfKind(CodeClassKind.Model) && parentClass.Parent is CodeClass ? "static " : string.Empty; //https://stackoverflow.com/questions/47541459/no-enclosing-instance-is-accessible-must-qualify-the-allocation-with-an-enclosi
        writer.WriteLine(JavaConventionService.AutoGenerationHeader);
        writer.WriteLine($"public {innerClassStatic}class {codeElement.Name}{typeParametersDeclaration}{derivation} {{");
        writer.IncreaseIndent();
        if (typeParameters.Length != 0)
        {
            // only the parameters this class's own deserializers consume own a factory field; parameters
            // only needed by a generic base are accepted by the constructor and forwarded to it
            var ownedParameters = typeParameters.Where(parameter => parentClass.Properties.Any(property => ReferencesTypeParameter(property.Type, parameter))).ToArray();
            foreach (var typeParameter in ownedParameters)
                writer.WriteLine($"private final ParsableFactory<{typeParameter.Name}> {JavaConventionService.GetFactoryFieldName(typeParameter)};");
            var forwardedParameterNames = (codeElement.Inherits?.GenericTypeParameterValues ?? Enumerable.Empty<CodeType>())
                .Select(static x => x.TypeDefinition as CodeTypeParameter)
                .OfType<CodeTypeParameter>()
                .Select(JavaConventionService.GetFactoryParameterName)
                .ToArray();
            writer.WriteLine($"public {codeElement.Name}({string.Join(", ", typeParameters.Select(x => $"@jakarta.annotation.Nonnull final ParsableFactory<{x.Name}> {JavaConventionService.GetFactoryParameterName(x)}"))}) {{");
            writer.IncreaseIndent();
            if (forwardedParameterNames.Length != 0)
                writer.WriteLine($"super({string.Join(", ", forwardedParameterNames)});");
            foreach (var typeParameter in ownedParameters)
                writer.WriteLine($"this.{JavaConventionService.GetFactoryFieldName(typeParameter)} = {JavaConventionService.GetFactoryParameterName(typeParameter)};");
            writer.CloseBlock();
        }
    }
    private string GetGenericArgumentsString(CodeType inherits, CodeClass parentClass) => inherits.GenericTypeParameterValues.Any() ?
        $"<{string.Join(", ", inherits.GenericTypeParameterValues.Select(x => conventions.GetTypeString(x, parentClass)))}>" :
        string.Empty;
    private static bool ReferencesTypeParameter(CodeTypeBase propertyType, CodeTypeParameter parameter) => propertyType switch
    {
        CodeType codeType when codeType.TypeDefinition is CodeTypeParameter current => current.Name.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase),
        CodeType codeType => codeType.GenericTypeParameterValues.Any(x => ReferencesTypeParameter(x, parameter)),
        _ => false,
    };
}
