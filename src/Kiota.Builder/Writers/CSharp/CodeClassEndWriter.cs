namespace Kiota.Builder.Writers.CSharp {
    public class CodeClassEndWriter : BaseElementWriter<CodeClass.ClassEnd, CSharpConventionService>
    {
        public CodeClassEndWriter(CSharpConventionService conventionService):base(conventionService){}
        public override void WriteCodeElement(CodeClass.ClassEnd codeElement, LanguageWriter writer)
        {
            writer.DecreaseIndent();
            writer.WriteLine("}");
            if(codeElement?.Parent?.Parent is CodeNamespace) {
                writer.DecreaseIndent();
                writer.WriteLine("}");
            }
        }
    }
}
