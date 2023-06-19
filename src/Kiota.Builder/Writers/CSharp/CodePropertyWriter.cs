using System;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.CSharp;
public class CodePropertyWriter : BaseElementWriter<CodeProperty, CSharpConventionService>
{
    public CodePropertyWriter(CSharpConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.ExistsInExternalBaseType) return;
        var propertyType = conventions.GetTypeString(codeElement.Type, codeElement);
        var isNullableReferenceType = !propertyType.EndsWith("?", StringComparison.OrdinalIgnoreCase)
                                      && codeElement.IsOfKind(
                                            CodePropertyKind.Custom,
                                            CodePropertyKind.QueryParameter,
                                            CodePropertyKind.SerializationHint);// Other property types are appropriately constructor initialized
        conventions.WriteShortDescription(codeElement.Documentation.Description, writer);
        var deprecationMessage = conventions.GetDeprecationInformation(codeElement);
        if (!string.IsNullOrEmpty(deprecationMessage))
            writer.WriteLine(deprecationMessage);
        if (isNullableReferenceType)
        {
            CSharpConventionService.WriteNullableOpening(writer);
            WritePropertyInternal(codeElement, writer, $"{propertyType}?");
            CSharpConventionService.WriteNullableMiddle(writer);
        }

        WritePropertyInternal(codeElement, writer, propertyType);// Always write the normal way

        if (isNullableReferenceType)
            CSharpConventionService.WriteNullableClosing(writer);
    }

    private void WritePropertyInternal(CodeProperty codeElement, LanguageWriter writer, string propertyType)
    {
        if (codeElement.Parent is not CodeClass parentClass) throw new InvalidOperationException("The parent of a property should be a class");
        var backingStoreProperty = parentClass.GetBackingStoreProperty();
        var setterAccessModifier = codeElement.ReadOnly && codeElement.Access > AccessModifier.Private ? "private " : string.Empty;
        var simpleBody = $"get; {setterAccessModifier}set;";
        var defaultValue = string.Empty;
        switch (codeElement.Kind)
        {
            case CodePropertyKind.RequestBuilder:
                writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} {propertyType} {codeElement.Name.ToFirstCharacterUpperCase()} {{ get =>");
                writer.IncreaseIndent();
                conventions.AddRequestBuilderBody(parentClass, propertyType, writer);
                writer.DecreaseIndent();
                writer.WriteLine("}");
                break;
            case CodePropertyKind.AdditionalData when backingStoreProperty != null:
            case CodePropertyKind.Custom when backingStoreProperty != null:
                var backingStoreKey = codeElement.WireName;
                writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} {propertyType} {codeElement.Name.ToFirstCharacterUpperCase()} {{");
                writer.IncreaseIndent();
                writer.WriteLine($"get {{ return {backingStoreProperty.Name.ToFirstCharacterUpperCase()}?.Get<{propertyType}>(\"{backingStoreKey}\"); }}");
                writer.WriteLine($"set {{ {backingStoreProperty.Name.ToFirstCharacterUpperCase()}?.Set(\"{backingStoreKey}\", value); }}");
                writer.DecreaseIndent();
                writer.WriteLine("}");
                break;
            case CodePropertyKind.QueryParameter when codeElement.IsNameEscaped:
                writer.WriteLine($"[QueryParameter(\"{codeElement.SerializationName}\")]");
                goto default;
            case CodePropertyKind.QueryParameters:
                defaultValue = $" = new {propertyType}();";
                goto default;
            default:
                writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} {propertyType} {codeElement.Name.ToFirstCharacterUpperCase()} {{ {simpleBody} }}{defaultValue}");
                break;
        }
    }
}
