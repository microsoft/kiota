namespace Kiota.Builder.Writers.Java {
    public class CodeBlockEndWriter : ICodeElementWriter<BlockEnd>
    {
        public void WriteCodeElement(BlockEnd codeElement, LanguageWriter writer)
        {
            writer.CloseBlock();
        }
    }
}
