using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Php
{
    public class CodeBlockEndWriter: ICodeElementWriter<BlockEnd>
    {
        public void WriteCodeElement(BlockEnd codeElement, LanguageWriter writer)
        {
            if(codeElement.Parent is CodeNamespace) return;
            writer.CloseBlock(string.Empty);
        }
    }
}
