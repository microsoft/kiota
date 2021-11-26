namespace Kiota.Builder.Writers.Php
{
    public class CodeClassEndWriter: ICodeElementWriter<CodeClass.End>
    {
        public void WriteCodeElement(CodeClass.End codeElement, LanguageWriter writer)
        {
            writer.CloseBlock();
        }
    }
}
