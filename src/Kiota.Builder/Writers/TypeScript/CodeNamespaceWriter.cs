namespace  Kiota.Builder.Writers.TypeScript {
    public class CodeNamespaceWriter : BaseElementWriter<CodeNamespace, TypeScriptConventionService>
    {
        public CodeNamespaceWriter(TypeScriptConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeNamespace codeElement, LanguageWriter writer)
        {
        }
    }
}
