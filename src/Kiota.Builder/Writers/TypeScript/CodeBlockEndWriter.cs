namespace Kiota.Builder.Writers.TypeScript {
    public class CodeBlockEndWriter : ICodeElementWriter<BlockEnd>
    {
        public void WriteCodeElement(BlockEnd codeElement, LanguageWriter writer)
        {
            if(codeElement.Parent is CodeNamespace) return;
            writer.CloseBlock();
        }
    }
}
