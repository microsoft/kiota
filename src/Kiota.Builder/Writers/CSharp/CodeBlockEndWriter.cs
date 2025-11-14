using System;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.CSharp;

public class CodeBlockEndWriter : BaseElementWriter<BlockEnd, CSharpConventionService>
{
    public CodeBlockEndWriter(CSharpConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(BlockEnd codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.CloseBlock();
        if (codeElement?.Parent is CodeClass codeClass && codeClass.Parent is CodeNamespace)
        {
            writer.CloseBlock();
            conventions.WritePragmaRestore(writer, CSharpConventionService.CS0618);
        }
    }
}
