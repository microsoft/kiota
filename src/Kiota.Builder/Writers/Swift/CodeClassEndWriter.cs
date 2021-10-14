namespace Kiota.Builder.Writers.Swift {
    public class CodeClassEndWriter : ICodeElementWriter<CodeClass.End>
    {
        public void WriteCodeElement(CodeClass.End codeElement, LanguageWriter writer)
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
