namespace  Kiota.Builder.Writers.CSharp {
    public class CodeNamespaceWriter : BaseElementWriter<CodeNamespace, CSharpConventionService>
    {
        public CodeNamespaceWriter(CSharpConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeNamespace codeElement, LanguageWriter writer)
        {
        }
    }
}
