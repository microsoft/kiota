using System;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.TypeScript;
public class CodePropertyWriter : BaseElementWriter<CodeProperty, TypeScriptConventionService>
{
    public CodePropertyWriter(TypeScriptConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.ExistsInExternalBaseType)
            return;
        var returnType = conventions.GetTypeString(codeElement.Type, codeElement);
        var isFlagEnum = codeElement.Type is CodeType currentType && currentType.TypeDefinition is CodeEnum currentEnum && currentEnum.Flags;

        conventions.WriteShortDescription(codeElement.Documentation.Description, writer);
        switch (codeElement.Parent)
        {
            case CodeInterface:
                WriteCodePropertyForInterface(codeElement, writer, returnType, isFlagEnum);
                break;
            default:
                WriteCodePropertyForClass(codeElement, writer, returnType, isFlagEnum);
                break;
        }
    }

    private static void WriteCodePropertyForInterface(CodeProperty codeElement, LanguageWriter writer, string returnType, bool isFlagEnum)
    {
        writer.WriteLine($"{codeElement.Name.ToFirstCharacterLowerCase()}?: {returnType}{(isFlagEnum ? "[]" : string.Empty)}{(codeElement.Type.IsNullable ? " | undefined" : string.Empty)};");
    }

    private void WriteCodePropertyForClass(CodeProperty codeElement, LanguageWriter writer, string returnType, bool isFlagEnum)
    {
        switch (codeElement.Kind)
        {
            case CodePropertyKind.RequestBuilder:
                writer.StartBlock($"{conventions.GetAccessModifier(codeElement.Access)} get {codeElement.Name.ToFirstCharacterLowerCase()}(): {returnType} {{");
                if (codeElement.Parent is CodeClass parentClass)
                    conventions.AddRequestBuilderBody(parentClass, returnType, writer);
                writer.CloseBlock();
                break;
            default:
                writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} {codeElement.NamePrefix}{codeElement.Name.ToFirstCharacterLowerCase()}{(codeElement.Type.IsNullable ? "?" : string.Empty)}: {returnType}{(isFlagEnum ? "[]" : string.Empty)}{(codeElement.Type.IsNullable ? " | undefined" : string.Empty)};");
                break;
        }
    }
}
