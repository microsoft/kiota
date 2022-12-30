using System;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace  Kiota.Builder.Writers.Ruby;
public class CodeNamespaceWriter : BaseElementWriter<CodeNamespace, RubyConventionService>
{
    public CodeNamespaceWriter(RubyConventionService conventionService) : base(conventionService){}
    public override void WriteCodeElement(CodeNamespace codeElement, LanguageWriter writer)
    {
        foreach(var childModel in codeElement.GetChildElements(true).OfType<CodeEnum>().OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase))
            writer.WriteLine($"require_relative '{childModel.Name.ToSnakeCase()}'");
        NamespaceClassNamesProvider.WriteClassesInOrderOfInheritance(codeElement, x => writer.WriteLine($"require_relative '{x.ToSnakeCase()}'"));
        writer.StartBlock($"module {codeElement.Name.NormalizeNameSpaceName("::")}");
        writer.CloseBlock("end");
    }

}
