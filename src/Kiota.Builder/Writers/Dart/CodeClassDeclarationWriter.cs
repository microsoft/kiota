using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.PathSegmenters;
using Microsoft.Kiota.Abstractions.Extensions;

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

        addImportsForDiscriminatorTypes(codeElement);

        var currentNamespace = codeElement.GetImmediateParentOfType<CodeNamespace>();

        var relativeImportManager = new RelativeImportManager(
            "keyhub", '.', (writer.PathSegmenter as DartPathSegmenter)!.GetRelativeFileName);

        if (codeElement.Parent?.Parent is CodeNamespace)
        {
            codeElement.Usings
                .Where(x => (x.Declaration?.IsExternal ?? true) || !x.Declaration.Name.Equals(codeElement.Name, StringComparison.OrdinalIgnoreCase)) // needed for circular requests patterns like message folder
                .Where(static x => x.IsExternal)
                .Select(x => $"import 'package:{x.Declaration!.Name}.dart';")
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static x => x, StringComparer.Ordinal)
                .ToList()
                .ForEach(x => writer.WriteLine(x));

            foreach (var relativePath in codeElement.Usings
                    .Where(static x => !x.IsExternal)
                    .DistinctBy(static x => $"{x.Name}{x.Declaration?.Name}", StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.Declaration?.Name?.StartsWith('.') ?? false ?
                        (string.Empty, string.Empty, x.Declaration.Name) :
                        relativeImportManager.GetRelativeImportPathForUsing(x, currentNamespace))
                    .Select(static x => x.Item3)
                    .Distinct()
                    .Order(StringComparer.OrdinalIgnoreCase))
                writer.WriteLine($"import '{relativePath.ToSnakeCase()}.dart';");

            writer.WriteLine();

        }

        var derivedTypes = (codeElement.Inherits is null ? Enumerable.Empty<string?>() : [conventions.GetTypeString(codeElement.Inherits, parentClass)]).ToArray();
        var derivation = derivedTypes.Length != 0 ? " extends " + derivedTypes.Aggregate(static (x, y) => $"{x}, {y}") : string.Empty;
        var implements = !codeElement.Implements.Any() ? string.Empty : $" implements {codeElement.Implements.Select(x => x.Name).Aggregate((x, y) => x + ", " + y)}";

        conventions.WriteLongDescription(parentClass, writer);
        conventions.WriteDeprecationAttribute(parentClass, writer);
        writer.StartBlock($"class {codeElement.Name.ToFirstCharacterUpperCase()}{derivation}{implements} {{");
    }
    /// <summary>
    /// Dart needs import statements for classes that are in the same folder.
    /// </summary
    void addImportsForDiscriminatorTypes(ClassDeclaration classDeclaration)
    {

       var parent = classDeclaration.Parent as CodeClass;
        var methods = parent!.GetMethodsOffKind(CodeMethodKind.Factory);
        var method = methods?.FirstOrDefault();
        if (method != null && method.Parent is CodeElement codeElement && method.Parent is IDiscriminatorInformationHolder)
        {
            var discriminatorInformation = (method.Parent as IDiscriminatorInformationHolder)!.DiscriminatorInformation;
            var discriminatorMappings = discriminatorInformation.DiscriminatorMappings;
            foreach (var discriminatorMapping in discriminatorMappings)
            {
                var className = discriminatorMapping.Value.Name;
                classDeclaration.AddUsings(new CodeUsing
                {
                    Name = className,
                    Declaration = discriminatorMapping.Value
                });
            }
        }
    }
}
