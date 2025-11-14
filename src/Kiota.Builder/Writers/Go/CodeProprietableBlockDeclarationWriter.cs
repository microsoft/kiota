using System;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Go;

public abstract class CodeProprietableBlockDeclarationWriter<T> : BaseElementWriter<T, GoConventionService>
    where T : ProprietableBlockDeclaration
{
    protected CodeProprietableBlockDeclarationWriter(GoConventionService conventionService) : base(conventionService) { }

    public override void WriteCodeElement(T codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.Parent?.Parent is CodeNamespace ns)
        {
            // always add a comment to the top of the file to indicate it's generated
            conventions.WriteGeneratorComment(writer);
            writer.WriteLine($"package {ns.Name.GetLastNamespaceSegment().Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)}");
            var importSegments = codeElement
                                .Usings
                                .Where(x => x.Declaration != null && !x.Declaration.IsExternal && !x.Name.Equals(ns.Name, StringComparison.OrdinalIgnoreCase) &&
                                        x.Declaration.TypeDefinition?.GetImmediateParentOfType<CodeNamespace>() != ns)
                                .Select(static x => x.GetInternalNamespaceImport())
                                .Select(static x => new Tuple<string, string>(x.GetNamespaceImportSymbol(), x))
                                .Distinct()
                                .Union(codeElement
                                    .Usings
                                    .Union(codeElement.Parent is CodeClass currentClass ? currentClass.InnerClasses.SelectMany(static x => x.Usings) : Enumerable.Empty<CodeUsing>())
                                    .Where(static x => x.Declaration != null && x.Declaration.IsExternal)
                                    .Select(static x => new Tuple<string, string>(x.Name.StartsWith('*') ? x.Name[1..] : x.Declaration!.Name.GetNamespaceImportSymbol(), x.Declaration!.Name))
                                    .Distinct())
                                .OrderBy(static x => x.Item2.Count(static y => y == '/'))
                                .ThenBy(static x => x)
                                .ToList();
            if (importSegments.Count != 0)
            {
                writer.WriteLines(string.Empty, "import (");
                writer.IncreaseIndent();
                importSegments.ForEach(x => writer.WriteLine(x.Item1.Equals(x.Item2, StringComparison.Ordinal) ? $"\"{x.Item2}\"" : $"{x.Item1} \"{x.Item2}\""));
                writer.DecreaseIndent();
                writer.WriteLines(")", string.Empty);
            }
        }
        WriteTypeDeclaration(codeElement, writer);
    }
    protected abstract void WriteTypeDeclaration(T codeElement, LanguageWriter writer);
}
