using System;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.TypeScript;

public class CodeEnumObjectWriter : BaseElementWriter<CodeEnumObject, TypeScriptConventionService>
{
    public CodeEnumObjectWriter(TypeScriptConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeEnumObject codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        var objectName = codeElement.Name.ToFirstCharacterUpperCase();
        writer.WriteLine($"export type {codeElement.Parent?.Name.ToFirstCharacterUpperCase()} = (typeof {objectName})[keyof typeof {objectName}];");
    }
}
