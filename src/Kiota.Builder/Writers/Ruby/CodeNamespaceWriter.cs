using Kiota.Builder.Extensions;

namespace  Kiota.Builder.Writers.Ruby {
    public class CodeNamespaceWriter : BaseElementWriter<CodeNamespace, RubyConventionService>
    {
        public CodeNamespaceWriter(RubyConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeNamespace codeElement, LanguageWriter writer)
        {
            writer.WriteLine($"module {codeElement.Name.NormalizeNameSpaceName("::")}");
            writer.WriteLine();
            writer.WriteLine("end");
        }
    }
}
