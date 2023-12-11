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
        var isFlagEnum = codeElement.Type is CodeType { TypeDefinition: CodeEnum { Flags: true } }
                         && !codeElement.Type.IsCollection;//collection of flagged enums are not supported/don't make sense

        conventions.WriteLongDescription(codeElement, writer);
        switch (codeElement.Parent)
        {
            case CodeInterface:
                WriteCodePropertyForInterface(codeElement, writer, returnType, isFlagEnum);
                break;
            case CodeClass:
                WriteCodePropertyForClass(codeElement, writer, returnType, isFlagEnum);
                break;
        }
    }

    private static void WriteCodePropertyForInterface(CodeProperty codeElement, LanguageWriter writer, string returnType, bool isFlagEnum)
    {
        writer.WriteLine($"{codeElement.Name.ToFirstCharacterLowerCase()}?: {returnType}{(isFlagEnum ? "[]" : string.Empty)};");
    }

    private void WriteCodePropertyForClass(CodeProperty codeElement, LanguageWriter writer, string returnType, bool isFlagEnum)
    {//TODO double check we still need this
        switch (codeElement.Kind)
        {
            case CodePropertyKind.ErrorMessageOverride:
                throw new InvalidOperationException($"Primary message mapping is done in deserializer function in TypeScript.");
            case CodePropertyKind.RequestBuilder:
                throw new InvalidOperationException($"Request builder property is implemented via a constant in TypeScript.");
            default:
                writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} {codeElement.NamePrefix}{codeElement.Name.ToFirstCharacterLowerCase()}{(codeElement.Type.IsNullable ? "?" : string.Empty)}: {returnType}{(isFlagEnum ? "[]" : string.Empty)};");
                break;
        }
    }
}
