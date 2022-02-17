namespace Kiota.Builder.Writers.Go {
    public class CodeBlockEndWriter : ICodeElementWriter<BlockEnd>
    {
        public void WriteCodeElement(BlockEnd codeElement, LanguageWriter writer)
        {
            if(codeElement.Parent is CodeNamespace || codeElement.Parent is CodeEnum) return;
            writer.CloseBlock();
        }
    }
}
