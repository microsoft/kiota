using System;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Dart;
public class CodePropertyWriter : BaseElementWriter<CodeProperty, DartConventionService>
{
    public CodePropertyWriter(DartConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);

        var propertyName = codeElement.Name.ToFirstCharacterUpperCase();
        writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} {conventions.TranslateType(codeElement.Type)} {propertyName} {{");
        writer.IncreaseIndent();
        writer.WriteLine("get; set;");
        writer.DecreaseIndent();
        writer.WriteLine("}");
    }
}
