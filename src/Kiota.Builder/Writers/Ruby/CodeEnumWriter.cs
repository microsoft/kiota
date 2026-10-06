using System;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Ruby;

public class CodeEnumWriter : BaseElementWriter<CodeEnum, RubyConventionService>
{
    public CodeEnumWriter(RubyConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteLine(RubyConventionService.FrozenStringLiteralComment);
        if (!codeElement.Options.Any())
            return;
        writer.WriteLine();
        if (codeElement.Parent is CodeNamespace ns)
            conventions.WriteNamespaceModules(ns, writer);
        conventions.WriteShortDescription(codeElement, writer);
        writer.StartBlock($"{codeElement.Name.ToFirstCharacterUpperCase()} = {{");
        // the value is what goes on the wire, the key is what Ruby code looks the member up by
        var options = codeElement.Options.Select(static x => $"{x.Name.ToFirstCharacterUpperCase()}: {RubyConventionService.ToRubyStringLiteral(x.WireName)}").ToArray();
        for (var i = 0; i < options.Length; i++)
            writer.WriteLine(i < options.Length - 1 ? $"{options[i]}," : options[i]);
        writer.CloseBlock("}.freeze");
        if (codeElement.Parent is CodeNamespace ns2)
            conventions.WriteNamespaceClosing(ns2, writer);
    }
}
