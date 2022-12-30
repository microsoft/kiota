using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace  Kiota.Builder.Writers.Ruby;
public class CodeNamespaceWriter : BaseElementWriter<CodeNamespace, RubyConventionService>
{
    public CodeNamespaceWriter(RubyConventionService conventionService) : base(conventionService){}
    public override void WriteCodeElement(CodeNamespace codeElement, LanguageWriter writer)
    {
        foreach(var childModel in codeElement.GetChildElements(true).OfType<CodeEnum>())
            writer.WriteLine($"require_relative '{childModel.Name.ToSnakeCase()}'");
        var sortedClassNames = NamespaceClassNamesProvider.SortClassesInOrderOfInheritance(codeElement.Classes);
        foreach (var className in sortedClassNames)
            writer.WriteLine($"require_relative '{className.ToSnakeCase()}'");
        writer.StartBlock($"module {codeElement.Name.NormalizeNameSpaceName("::")}");
        writer.CloseBlock("end");
    }

}
