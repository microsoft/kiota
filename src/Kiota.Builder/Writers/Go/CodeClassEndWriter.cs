namespace Kiota.Builder.Writers.Go {
    public class CodeClassEndWriter : ICodeElementWriter<CodeClass.End>
    {
        public void WriteCodeElement(CodeClass.End codeElement, LanguageWriter writer)
        {
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
    }
}
