using Kiota.Builder.Extensions;

namespace  Kiota.Builder.Writers.Python {
    public class CodeNameSpaceWriter : BaseElementWriter<CodeNamespace, PythonConventionService>
    {
        public CodeNameSpaceWriter(PythonConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeNamespace codeElement, LanguageWriter writer)
        {
            writer.WriteLine(); //Add single blank line in init files
        }
    }
}
