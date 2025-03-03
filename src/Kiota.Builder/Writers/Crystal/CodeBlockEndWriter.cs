using System;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Crystal;
public class CodeBlockEndWriter : BaseElementWriter<BlockEnd, CrystalConventionService>
{
    public CodeBlockEndWriter(CrystalConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(BlockEnd codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.CloseBlock();
        if (codeElement?.Parent is CodeClass codeClass && codeClass.Parent is CodeNamespace)
        {
            writer.DecreaseIndent();
            writer.WriteLine("end");
        }
    }
}
