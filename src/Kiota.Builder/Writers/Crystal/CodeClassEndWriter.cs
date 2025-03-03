using System;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Crystal;
public class CodeClassEndWriter : BaseElementWriter<BlockEnd, CrystalConventionService>
{
    public CodeClassEndWriter(CrystalConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(BlockEnd codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteLine("end");
    }
}
