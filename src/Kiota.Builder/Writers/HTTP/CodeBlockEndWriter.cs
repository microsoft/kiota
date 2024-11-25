using System;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Http;
public class CodeBlockEndWriter : ICodeElementWriter<BlockEnd>
{
    public void WriteCodeElement(BlockEnd codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
    }
}
