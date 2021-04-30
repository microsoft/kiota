namespace Kiota.Builder.Writers.Java {
    public class CodeTypeWriter : BaseElementWriter<CodeType, JavaConventionService>
    {
        public CodeTypeWriter(JavaConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeType codeElement, LanguageWriter writer)
        {
            writer.Write(conventions.GetTypeString(codeElement), includeIndent: false);
        }
    }
}
