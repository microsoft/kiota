using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Rust;

public class CodeFileBlockEndWriter : ICodeElementWriter<CodeFileBlockEnd>
{
    public void WriteCodeElement(CodeFileBlockEnd codeElement, LanguageWriter writer)
    {
        // No file-level closing needed in Rust
    }
}
