using System;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Swift;
public class CodeBlockEndWriter : ICodeElementWriter<BlockEnd>
{
    public void WriteCodeElement(BlockEnd codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        writer.CloseBlock();
        if (codeElement?.Parent?.Parent is CodeNamespace && !(codeElement.Parent is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.BarrelInitializer)))
        {
            writer.CloseBlock();
        }
    }
}
