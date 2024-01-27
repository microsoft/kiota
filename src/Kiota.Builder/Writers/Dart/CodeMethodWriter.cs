using System;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Dart;
public class CodeMethodWriter : BaseElementWriter<CodeMethod, DartConventionService>
{
    public CodeMethodWriter(DartConventionService conventionService) : base(conventionService)
    {
    }

    public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);

        var returnType = conventions.TranslateType(codeElement.ReturnType);
        var methodName = codeElement.Name.ToFirstCharacterUpperCase();
        writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} {returnType} {methodName}() {{");
        writer.IncreaseIndent();
        writer.WriteLine("throw \"not implemented\";");
        writer.DecreaseIndent();
        writer.WriteLine("}");
    }
}
