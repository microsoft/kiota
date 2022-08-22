using System;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Go;
public abstract class CodeProprietableBlockDeclarationWriter<T> : BaseElementWriter<T, GoConventionService> 
    where T : ProprietableBlockDeclaration
{
    protected CodeProprietableBlockDeclarationWriter(GoConventionService conventionService) : base(conventionService) {}

    public override void WriteCodeElement(T codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement, nameof(codeElement));
        ArgumentNullException.ThrowIfNull(writer, nameof(writer));
        if (codeElement.Parent?.Parent is CodeNamespace ns)
        {
            writer.WriteLine($"package {ns.Name.GetLastNamespaceSegment().Replace("-", string.Empty)}");
            var importSegments = codeElement
                                .Usings
                                .Where(x => !x.Declaration.IsExternal && !x.Name.Equals(ns.Name, StringComparison.OrdinalIgnoreCase))
                                .Where(x => x.Declaration.TypeDefinition?.GetImmediateParentOfType<CodeNamespace>() != ns)
                                .Select(static x => x.GetInternalNamespaceImport())
                                .Select(static x => new Tuple<string, string>(x.GetNamespaceImportSymbol(), x))
                                .Distinct()
                                .Union(codeElement
                                    .Usings
                                    .Where(static x => x.Declaration.IsExternal)
                                    .Select(static x => new Tuple<string, string>(x.Name.StartsWith("*") ? x.Name[1..] : x.Declaration.Name.GetNamespaceImportSymbol(), x.Declaration.Name))
                                    .Distinct())
                                .OrderBy(static x => x.Item2.Count(static y => y == '/'))
                                .ThenBy(static x => x)
                                .ToList();
            if (importSegments.Any())
            {
                writer.WriteLines(string.Empty, "import (");
                writer.IncreaseIndent();
                importSegments.ForEach(x => writer.WriteLine(x.Item1.Equals(x.Item2) ? $"\"{x.Item2}\"" : $"{x.Item1} \"{x.Item2}\""));
                writer.DecreaseIndent();
                writer.WriteLines(")", string.Empty);
            }
        }
        WriteTypeDeclaration(codeElement, writer);
    }
    protected abstract void WriteTypeDeclaration(T codeElement, LanguageWriter writer);
}
