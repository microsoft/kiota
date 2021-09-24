namespace Kiota.Builder.Writers {
    public class CodeTypeWriter : BaseElementWriter<CodeType, ILanguageConventionService>
    {
        public CodeTypeWriter(ILanguageConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeType codeElement, LanguageWriter writer)
        {
            writer.Write(conventions.GetTypeString(codeElement, codeElement), includeIndent: false);
        }
    }
}
