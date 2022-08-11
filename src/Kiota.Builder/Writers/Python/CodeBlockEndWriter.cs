namespace Kiota.Builder.Writers.Python;
public class CodeBlockEndWriter : ICodeElementWriter<BlockEnd>
{
    public void WriteCodeElement(BlockEnd codeElement, LanguageWriter writer)
    {
        if(codeElement.Parent is CodeNamespace) return;
        writer.CloseBlock(string.Empty);
    }
}
