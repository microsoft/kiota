namespace  Kiota.Builder.Writers.Ruby {
    public class CodeClassEndWriter : ICodeElementWriter<CodeClass.End>
    {
        public void WriteCodeElement(CodeClass.End codeElement, LanguageWriter writer)
        {
            writer.DecreaseIndent();
            writer.WriteLine("end");
        }
    }
}
