using System;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Go;

namespace Kiota.Builder.Writers.TypeScript;
public class CodeConstantWriter : BaseElementWriter<CodeConstant, TypeScriptConventionService>
{
    public CodeConstantWriter(TypeScriptConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeConstant codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.OriginalCodeElement is null) throw new InvalidOperationException("Original CodeElement cannot be null");
        switch (codeElement.Kind)
        {
            case CodeConstantKind.QueryParametersMapper:
                WriteQueryParametersMapperConstant(codeElement, writer);
                break;
            case CodeConstantKind.EnumObject:
                WriteEnumObjectConstant(codeElement, writer);
                break;
        }
    }
    private static void WriteQueryParametersMapperConstant(CodeConstant codeElement, LanguageWriter writer)
    {
        if (codeElement.OriginalCodeElement is not CodeInterface codeInterface) throw new InvalidOperationException("Original CodeElement cannot be null");
        writer.StartBlock($"const {codeElement.Name.ToFirstCharacterLowerCase()}: Record<string, string> = {{");
        foreach (var property in codeInterface
                                                .Properties
                                                .OfKind(CodePropertyKind.QueryParameter)
                                                .Where(static x => !string.IsNullOrEmpty(x.SerializationName))
                                                .OrderBy(static x => x.SerializationName, StringComparer.OrdinalIgnoreCase))
        {
            writer.WriteLine($"\"{property.Name.ToFirstCharacterLowerCase()}\": \"{property.SerializationName}\",");
        }
        writer.CloseBlock("};");
    }
    private void WriteEnumObjectConstant(CodeConstant codeElement, LanguageWriter writer)
    {
        if (codeElement.OriginalCodeElement is not CodeEnum codeEnum) throw new InvalidOperationException("Original CodeElement cannot be null");
        if (!codeEnum.Options.Any())
            return;
        conventions.WriteLongDescription(codeEnum, writer);
        writer.WriteLine($"export const {codeElement.Name.ToFirstCharacterUpperCase()} = {{");
        writer.IncreaseIndent();
        codeEnum.Options.ToList().ForEach(x =>
        {
            conventions.WriteShortDescription(x.Documentation.Description, writer);
            writer.WriteLine($"{x.Name.ToFirstCharacterUpperCase()}: \"{x.WireName}\",");
        });
        writer.DecreaseIndent();
        writer.WriteLine("}  as const;");
    }
}
