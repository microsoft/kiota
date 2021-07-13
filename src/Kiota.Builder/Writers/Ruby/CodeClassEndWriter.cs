namespace Kiota.Builder.Writers.Ruby {
    public class CodeClassEndWriter : BaseElementWriter<CodeClass.End, RubyConventionService>
    {
        public CodeClassEndWriter(RubyConventionService conventionService):base(conventionService){}
        public override void WriteCodeElement(CodeClass.End codeElement, LanguageWriter writer)
        {
            const string end = "end";
            writer.DecreaseIndent();
            writer.WriteLine(end);
            if(codeElement?.Parent?.Parent is CodeNamespace) {
                writer.DecreaseIndent();
                writer.WriteLine(end);
            }
        }
    }
}
