namespace Kiota.Builder.Writers.Java {
    public class CodeClassEndWriter : ICodeElementWriter<CodeClass.End>
    {
        public void WriteCodeElement(CodeClass.End codeElement, LanguageWriter writer)
        {
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
    }
}
