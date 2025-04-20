using System;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.AL;
public class CodeBlockEndWriter : BaseElementWriter<BlockEnd, ALConventionService>
{
    public CodeBlockEndWriter(ALConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(BlockEnd codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(codeElement);
        if (codeElement.ParentIsSkipped()) return;
        if (codeElement.Parent?.Name == "AppJson")
            return;
        writer.CloseBlock();
        // if (codeElement?.Parent is CodeClass codeClass && codeClass.Parent is CodeNamespace)
        // {
        //     writer.CloseBlock();
        //     conventions.WritePragmaRestore(writer, ALConventionService.CS0618);
        // }
    }
}
