namespace Kiota.Builder.Writers.Go {
    public class CodeBlockEndWriter : ICodeElementWriter<BlockEnd>
    {
        public void WriteCodeElement(BlockEnd codeElement, LanguageWriter writer)
        {
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
    }
}
