using System;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.PathSegmenters;

namespace Kiota.Builder.Writers.Dart;
public class CodeClassDeclarationWriter : BaseElementWriter<ClassDeclaration, DartConventionService>
{
    private readonly RelativeImportManager relativeImportManager;

    public CodeClassDeclarationWriter(DartConventionService conventionService, string clientNamespaceName, DartPathSegmenter pathSegmenter) : base(conventionService)
    {
        ArgumentNullException.ThrowIfNull(pathSegmenter);
        relativeImportManager = new RelativeImportManager(clientNamespaceName, '.', pathSegmenter.GetRelativeFileName);
    }

    public override void WriteCodeElement(ClassDeclaration codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);

        if (codeElement.Parent is not CodeClass parentClass)
            throw new InvalidOperationException($"The provided code element {codeElement.Name} doesn't have a parent of type {nameof(CodeClass)}");

        var currentNamespace = codeElement.GetImmediateParentOfType<CodeNamespace>();

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
                        (string.Empty, x.Alias, x.Declaration.Name) :
                        relativeImportManager.GetRelativeImportPathForUsing(x, currentNamespace))
                    .OrderBy(static x => x.Item3, StringComparer.Ordinal))
                writer.WriteLine($"import '{relativePath.Item3}.dart'{getAlias(relativePath.Item2)};");

            writer.WriteLine();

        }

        var derivedTypes = (codeElement.Inherits is null ? Enumerable.Empty<string?>() : [conventions.GetTypeString(codeElement.Inherits, parentClass)]).ToArray();
        var derivation = derivedTypes.Length != 0 ? " extends " + derivedTypes.Aggregate(static (x, y) => $"{x}, {y}") : string.Empty;
        var implements = !codeElement.Implements.Any() ? string.Empty : $" implements {codeElement.Implements.Select(x => x.Name).Aggregate((x, y) => x + ", " + y)}";

        conventions.WriteLongDescription(parentClass, writer);
        conventions.WriteDeprecationAttribute(parentClass, writer);
        writer.StartBlock($"class {codeElement.Name}{derivation}{implements} {{");
    }

    private String getAlias(string alias)
    {
        return string.IsNullOrEmpty(alias) ? string.Empty : $" as {alias}";
    }
}
