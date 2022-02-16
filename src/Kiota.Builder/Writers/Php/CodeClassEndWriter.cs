namespace Kiota.Builder.Writers.Php
{
    public class CodeClassEndWriter: ICodeElementWriter<CodeClass.ClassEnd>
    {
        public void WriteCodeElement(CodeClass.ClassEnd codeElement, LanguageWriter writer)
        {
            writer.CloseBlock();
        }
    }
}
