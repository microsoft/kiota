using System;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using static Kiota.Builder.Writers.TypeScript.TypeScriptConventionService;

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
        var returnType = GetTypescriptTypeString(codeElement.Type, codeElement, inlineComposedTypeString: true);
        var isFlagEnum = codeElement.Type is CodeType { TypeDefinition: CodeEnum { Flags: true } }
                         && !codeElement.Type.IsCollection;//collection of flagged enums are not supported/don't make sense

        conventions.WriteLongDescription(codeElement, writer);
        switch (codeElement.Parent)
        {
            case CodeInterface:
                WriteCodePropertyForInterface(codeElement, writer, returnType, isFlagEnum);
                break;
            case CodeClass:
                throw new InvalidOperationException($"All properties are defined on interfaces in TypeScript.");
        }
    }
    private static void WriteCodePropertyForInterface(CodeProperty codeElement, LanguageWriter writer, string returnType, bool isFlagEnum)
    {
        switch (codeElement.Kind)
        {
            case CodePropertyKind.RequestBuilder:
                writer.WriteLine($"get {codeElement.Name.ToFirstCharacterLowerCase()}(): {returnType};");
                break;
            case CodePropertyKind.QueryParameter:
            case CodePropertyKind.AdditionalData:
                writer.WriteLine($"{codeElement.Name.ToFirstCharacterLowerCase()}?: {returnType}{(isFlagEnum ? "[]" : string.Empty)};");
                break;
            default:
                writer.WriteLine($"{codeElement.Name.ToFirstCharacterLowerCase()}?: {returnType}{(isFlagEnum ? "[]" : string.Empty)} | null;");
                break;
        }
    }
}
