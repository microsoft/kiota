﻿using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.TypeScript;
public class CodeEnumWriter : BaseElementWriter<CodeEnum, TypeScriptConventionService>
{
    public CodeEnumWriter(TypeScriptConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer)
    {
        if (!codeElement.Options.Any())
            return;

        conventions.WriteShortDescription(codeElement.Documentation.Description, writer);
        writer.WriteLine($"export enum {codeElement.Name.ToFirstCharacterUpperCase()} {{");
        writer.IncreaseIndent();
        codeElement.Options.ToList().ForEach(x =>
        {
            conventions.WriteShortDescription(x.Documentation.Description, writer);
            writer.WriteLine($"{x.Name.ToFirstCharacterUpperCase()} = \"{x.WireName}\",");
        });
    }
}
