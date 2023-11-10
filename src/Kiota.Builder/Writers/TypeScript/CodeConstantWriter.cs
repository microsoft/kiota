using System;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.TypeScript;
public class CodeConstantWriter : BaseElementWriter<CodeConstant, TypeScriptConventionService>
{
    public CodeConstantWriter(TypeScriptConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeConstant codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.OriginalInterface is null) throw new InvalidOperationException("Original interface cannot be null");
        writer.StartBlock($"const {codeElement.Name.ToFirstCharacterLowerCase()}: Record<string, string> = {{");
        foreach (var property in codeElement.OriginalInterface
                                                .Properties
                                                .OfKind(CodePropertyKind.QueryParameter)
                                                .Where(static x => !string.IsNullOrEmpty(x.SerializationName))
                                                .OrderBy(static x => x.SerializationName, StringComparer.OrdinalIgnoreCase))
        {
            writer.WriteLine($"\"{property.Name.ToFirstCharacterLowerCase()}\": \"{property.SerializationName}\",");
        }
        writer.CloseBlock("};");
    }
}
