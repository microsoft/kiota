﻿using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Ruby;
public class CodeEnumWriter : BaseElementWriter<CodeEnum, RubyConventionService>
{
    public CodeEnumWriter(RubyConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer)
    {
        if (!(codeElement?.Options.Any() ?? false))
            return;
        if (codeElement.Parent is CodeNamespace ns)
            conventions.WriteNamespaceModules(ns, writer);
        conventions.WriteShortDescription(codeElement.Documentation.Description, writer);
        writer.StartBlock($"{codeElement.Name.ToFirstCharacterUpperCase()} = {{");
        codeElement.Options.ToList().ForEach(x => writer.WriteLine($"{x.Name.ToFirstCharacterUpperCase()}: :{x.Name.ToFirstCharacterUpperCase()},"));
        writer.CloseBlock();
        if (codeElement.Parent is CodeNamespace ns2)
            conventions.WriteNamespaceClosing(ns2, writer);
    }
}
