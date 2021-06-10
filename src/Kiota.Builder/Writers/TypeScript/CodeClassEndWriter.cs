namespace Kiota.Builder.Writers.TypeScript {
    public class CodeClassEndWriter : ICodeElementWriter<CodeClass.End>
    {
        public void WriteCodeElement(CodeClass.End codeElement, LanguageWriter writer)
        {
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
    }
}
