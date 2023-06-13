using System;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.TypeScript;
public class CodeBlockEndWriter : ICodeElementWriter<BlockEnd>
{
    public void WriteCodeElement(BlockEnd codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.Parent is CodeNamespace) return;
        writer.CloseBlock();
    }
}
