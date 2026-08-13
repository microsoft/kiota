using System;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.PathSegmenters;

namespace Kiota.Builder.Writers.Ruby;

public class CodeClassDeclarationWriter : BaseElementWriter<ClassDeclaration, RubyConventionService>
{
    private readonly RelativeImportManager relativeImportManager;
    public CodeClassDeclarationWriter(RubyConventionService conventionService, string clientNamespaceName, RubyPathSegmenter pathSegmenter) : base(conventionService)
    {
        ArgumentNullException.ThrowIfNull(pathSegmenter);
        relativeImportManager = new RelativeImportManager(
                                    clientNamespaceName,
                                    '.',
                                    pathSegmenter.GetRelativeFileName);
    }


    public override void WriteCodeElement(ClassDeclaration codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        var currentNamespace = codeElement.GetImmediateParentOfType<CodeNamespace>();
        if (codeElement.Parent?.Parent is not CodeClass)
        {
            foreach (var codeUsing in codeElement.Usings
                                        .Where(static x => x.IsExternal)
                                        .Select(static x => x.Declaration?.Name?.ToSnakeCase())
                                        .Where(static x => !string.IsNullOrEmpty(x))
                                        .GroupBy(static x => x)
                                        .Select(static x => x.Key)
                                        .Order(StringComparer.OrdinalIgnoreCase))
                writer.WriteLine($"require '{codeUsing}'");

            foreach (var relativePath in codeElement.Usings
                                        .Where(static x => !x.IsExternal)
                                        .DistinctBy(static x => $"{x.Name}{x.Declaration?.Name}", StringComparer.OrdinalIgnoreCase)
                                        .Select(x => x.Declaration?.Name?.StartsWith('.') ?? false ?
                                            (string.Empty, string.Empty, x.Declaration.Name) :
                                            relativeImportManager.GetRelativeImportPathForUsing(x, currentNamespace))
                                        .Select(static x => x.Item3)
                                        .Distinct()
                                        .Order(StringComparer.OrdinalIgnoreCase))
                writer.WriteLine($"require_relative '{relativePath.ToSnakeCase()}'");
        }
        writer.WriteLine();
        if (codeElement.Parent?.Parent is CodeNamespace ns)
        {
            conventions.WriteNamespaceModules(ns, writer);
        }

        var derivation = codeElement.Inherits == null ? string.Empty : $" < {conventions.GetNormalizedNamespacePrefixForType(codeElement.Inherits)}{codeElement.Inherits.Name.ToFirstCharacterUpperCase()}";
        if (codeElement.Parent is CodeClass parentClass)
            conventions.WriteShortDescription(parentClass, writer);
        writer.StartBlock($"class {codeElement.Name.ToFirstCharacterUpperCase()}{derivation}");
        var mixins = !codeElement.Implements.Any() ? string.Empty : $"include {codeElement.Implements.Select(static x => x.Name).Aggregate(static (x, y) => x + ", " + y)}";
        writer.WriteLine($"{mixins}");
    }
}
