using System;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Go;
public class CodeBlockEndWriter : ICodeElementWriter<BlockEnd>
{
    public void WriteCodeElement(BlockEnd codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(codeElement);
        if (codeElement.Parent is CodeNamespace || codeElement.Parent is CodeEnum) return;
        writer.CloseBlock();
        if (codeElement.Parent is CodeInterface parentInterface && parentInterface.OriginalClass is not null && parentInterface.OriginalClass.IsErrorDefinition && parentInterface.GetPrimaryMessageCodePath(static x => x.Name.ToFirstCharacterUpperCase(), static x => x.Name.ToFirstCharacterUpperCase() + "()") is string primaryMessageCodePath && !string.IsNullOrEmpty(primaryMessageCodePath))
        {
            writer.StartBlock($"func (e *{parentInterface.OriginalClass.Name.ToFirstCharacterUpperCase()}) Error() string {{");
            writer.WriteLine($"return *(e.{primaryMessageCodePath})");
            writer.CloseBlock();
        }
    }
}
