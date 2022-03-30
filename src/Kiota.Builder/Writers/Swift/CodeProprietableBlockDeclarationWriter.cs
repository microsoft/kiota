using System;
using System.Linq;

namespace Kiota.Builder.Writers.Swift;
public abstract class CodeProprietableBlockDeclarationWriter<T> : BaseElementWriter<T, SwiftConventionService> 
    where T : ProprietableBlockDeclaration
{
    protected CodeProprietableBlockDeclarationWriter(SwiftConventionService conventionService) : base(conventionService) {}
    public override void WriteCodeElement(T codeElement, LanguageWriter writer)
    {
        if(codeElement == null) throw new ArgumentNullException(nameof(codeElement));
        if(writer == null) throw new ArgumentNullException(nameof(writer));
        if (codeElement.Parent?.Parent is CodeNamespace ns)
        {
            var importSegments = codeElement
                                    .Usings
                                    .Where(x => x.Declaration.IsExternal)
                                    .Select(x => x.Declaration.Name)
                                    .Distinct()
                                .OrderBy(x => x.Count(y => y == '.'))
                                .ThenBy(x => x)
                                .ToList();
            if (importSegments.Any())
            {
                importSegments.ForEach(x => writer.WriteLine($"import {x}"));
                writer.WriteLine(string.Empty);
            }
        }

        if(codeElement?.Parent?.Parent is CodeNamespace && !(codeElement.Parent is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.BarrelInitializer))) {
            writer.WriteLine($"extension {codeElement.Parent.Parent.Name} {{");
            writer.IncreaseIndent();
        }
        
        WriteTypeDeclaration(codeElement, writer);
    }
    protected abstract void WriteTypeDeclaration(T codeElement, LanguageWriter writer);
}
