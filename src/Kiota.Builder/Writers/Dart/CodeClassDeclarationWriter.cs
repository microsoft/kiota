using System;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Dart;

public class CodeClassDeclarationWriter : BaseElementWriter<ClassDeclaration, DartConventionService>
{
    public CodeClassDeclarationWriter(DartConventionService conventionService) : base(conventionService)
    {
    }

    public override void WriteCodeElement(ClassDeclaration codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteLine("class " + codeElement.Name + " {");
    }
}
