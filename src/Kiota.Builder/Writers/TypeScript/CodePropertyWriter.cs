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

        if (codeElement.Kind is CodePropertyKind.AdditionalData && codeElement.Parent is CodeInterface)
        {
            // additional data is already defined in the parent interface, no need to redefine it
            return;
        }
        var returnType = GetTypescriptTypeString(codeElement.Type, codeElement, inlineComposedTypeString: true);
        var isFlagEnum = codeElement.Type is CodeType { TypeDefinition: CodeEnum { Flags: true } }
                         && !codeElement.Type.IsCollection;//collection of flagged enums are not supported/don't make sense

        conventions.WriteLongDescription(codeElement, writer);
        switch (codeElement.Parent)
        {
            case CodeInterface:
                WriteCodePropertyForInterface(codeElement, writer, returnType, isFlagEnum, conventions.MakeRequiredPropertiesNonNullable);
                break;
            case CodeClass:
                throw new InvalidOperationException($"All properties are defined on interfaces in TypeScript.");
        }
    }
    private static void WriteCodePropertyForInterface(CodeProperty codeElement, LanguageWriter writer, string returnType, bool isFlagEnum, bool makeRequiredPropertiesNonNullable)
    {
        var collectionSuffix = isFlagEnum ? "[]" : string.Empty;
        switch (codeElement.Kind)
        {
            case CodePropertyKind.RequestBuilder:
                writer.WriteLine($"get {codeElement.Name.ToFirstCharacterLowerCase()}(): {returnType};");
                break;
            case CodePropertyKind.QueryParameter:
                writer.WriteLine($"{codeElement.Name.ToFirstCharacterLowerCase()}?: {returnType}{collectionSuffix};");
                break;
            default:
                // When enabled, a required property is non-optional (no `?`), and drops `| null` unless its
                // schema is explicitly nullable. Otherwise the historical optional + nullable form is kept.
                var suppressOptionalAndNull = makeRequiredPropertiesNonNullable && codeElement.IsRequired;
                var optionalMarker = suppressOptionalAndNull ? string.Empty : "?";
                var nullSuffix = suppressOptionalAndNull
                    ? (codeElement.Type.IsNullable ? " | null" : string.Empty)
                    : " | null";
                writer.WriteLine($"{codeElement.Name.ToFirstCharacterLowerCase()}{optionalMarker}: {returnType}{collectionSuffix}{nullSuffix};");
                break;
        }
    }
}
