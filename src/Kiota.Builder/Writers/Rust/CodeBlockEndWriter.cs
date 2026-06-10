using System.Linq;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Rust;

public class CodeBlockEndWriter : ICodeElementWriter<BlockEnd>
{
    public void WriteCodeElement(BlockEnd codeElement, LanguageWriter writer)
    {
        if (codeElement?.Parent is CodeNamespace) return;
        if (codeElement?.Parent is CodeEnum) return;
        if (codeElement?.Parent is CodeClass cls)
        {
            // skip nested classes (query params, config their parent impl is closed by the nested writer)
            if (cls.Parent is CodeClass) return;
            // if this class has query param children, the impl was already closed by the query param writer
            if (cls.InnerClasses.Any(static c => c.IsOfKind(CodeClassKind.QueryParameters)))
                return;
        }
        writer?.CloseBlock();
    }
}
