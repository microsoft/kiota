using System;
using System.Linq;

using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Swift;
public abstract class CodeProprietableBlockDeclarationWriter<T> : BaseElementWriter<T, SwiftConventionService>
    where T : ProprietableBlockDeclaration
{
    protected CodeProprietableBlockDeclarationWriter(SwiftConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(T codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.Parent?.Parent is CodeNamespace)
        {
            var importSegments = codeElement
                                    .Usings
                                    .Where(static x => x.Declaration != null && x.Declaration.IsExternal)
                                    .Select(static x => x.Declaration!.Name)
                                    .Distinct()
                                .OrderBy(static x => x.Count(static y => y == '.'))
                                .ThenBy(x => x)
                                .ToList();
            if (importSegments.Any())
            {
                importSegments.ForEach(x => writer.WriteLine($"import {x}"));
                writer.WriteLine(string.Empty);
            }
        }

        if (codeElement.Parent?.Parent is CodeNamespace && !(codeElement.Parent is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.BarrelInitializer)))
        {
            writer.WriteLine($"extension {codeElement.Parent.Parent.Name} {{");
            writer.IncreaseIndent();
        }

        WriteTypeDeclaration(codeElement, writer);
    }
    protected abstract void WriteTypeDeclaration(T codeElement, LanguageWriter writer);
}
