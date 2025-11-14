using System;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Ruby;

public class CodeBlockEndWriter : BaseElementWriter<BlockEnd, RubyConventionService>
{
    private const string End = "end";
    public CodeBlockEndWriter(RubyConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(BlockEnd codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.Parent is CodeEnum) return;
        writer.CloseBlock(End);
        if (codeElement?.Parent is CodeClass codeClass && codeClass.Parent is CodeNamespace ns)
        {
            conventions.WriteNamespaceClosing(ns, writer);
        }
    }
}
