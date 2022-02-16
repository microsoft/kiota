namespace Kiota.Builder.Writers.Go {
    public class CodeClassEndWriter : ICodeElementWriter<CodeClass.ClassEnd>
    {
        public void WriteCodeElement(CodeClass.ClassEnd codeElement, LanguageWriter writer)
        {
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
    }
}
