using System;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Go;

public class CodeBlockEndWriter : ICodeElementWriter<BlockEnd>
{
    public void WriteCodeElement(BlockEnd codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(codeElement);
        if (codeElement.Parent is CodeNamespace || codeElement.Parent is CodeEnum) return;
        writer.CloseBlock();
    }
}
