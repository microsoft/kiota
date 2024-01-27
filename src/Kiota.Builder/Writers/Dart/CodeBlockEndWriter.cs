using System;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Dart;
public class CodeBlockEndWriter : BaseElementWriter<BlockEnd, DartConventionService>
{
    public CodeBlockEndWriter(DartConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(BlockEnd codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.CloseBlock();
        if (codeElement?.Parent is CodeClass)
        {
            writer.CloseBlock();
        }
    }
}
