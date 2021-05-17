namespace  Kiota.Builder.Writers.TypeScript {
    public class CodeTypeWriter : BaseElementWriter<CodeType, TypeScriptConventionService>
    {
        public CodeTypeWriter(TypeScriptConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeType codeElement, LanguageWriter writer)
        {
            writer.Write(conventions.GetTypeString(codeElement), includeIndent: false);
        }
    }
}
