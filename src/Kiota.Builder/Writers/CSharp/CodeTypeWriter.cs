namespace Kiota.Builder.Writers.CSharp {
    public class CodeTypeWriter : BaseElementWriter<CodeType, CSharpConventionService>
    {
        public CodeTypeWriter(CSharpConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeType codeElement, LanguageWriter writer)
        {
            writer.Write(conventions.GetTypeString(codeElement), includeIndent: false);
        }
    }
}
