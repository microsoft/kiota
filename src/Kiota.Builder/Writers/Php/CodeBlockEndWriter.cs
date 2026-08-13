using System;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Php;

public class CodeBlockEndWriter : ICodeElementWriter<BlockEnd>
{
    public void WriteCodeElement(BlockEnd codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.CloseBlock();
    }
}
