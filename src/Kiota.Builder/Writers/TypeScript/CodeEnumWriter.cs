using System;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.TypeScript;

public class CodeEnumWriter : BaseElementWriter<CodeEnum, TypeScriptConventionService>
{
    public CodeEnumWriter(TypeScriptConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(codeElement.CodeEnumObject);
        if (!codeElement.Options.Any())
            return;
        var enumObjectName = codeElement.CodeEnumObject.Name.ToFirstCharacterUpperCase();
        writer.WriteLine($"export type {codeElement.Name.ToFirstCharacterUpperCase()} = (typeof {enumObjectName})[keyof typeof {enumObjectName}];");
    }
}
