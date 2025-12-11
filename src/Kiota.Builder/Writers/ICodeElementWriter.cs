using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers;

public interface ICodeElementWriter<T> where T : CodeElement
{
    void WriteCodeElement(T codeElement, LanguageWriter writer);
}
