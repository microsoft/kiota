using System;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Crystal;
public class CodePropertyWriter : BaseElementWriter<CodeProperty, CrystalConventionService>
{
    public CodePropertyWriter(CrystalConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.ExistsInExternalBaseType) return;
        var propertyType = conventions.GetTypeString(codeElement.Type, codeElement);
        var isNullableReferenceType = !propertyType.EndsWith('?')
                                      && codeElement.IsOfKind(
                                            CodePropertyKind.Custom,
                                            CodePropertyKind.QueryParameter);
        conventions.WriteShortDescription(codeElement, writer);
        conventions.WriteDeprecationAttribute(codeElement, writer);

        WritePropertyInternal(codeElement, writer, propertyType);
    }

    private void WritePropertyInternal(CodeProperty codeElement, LanguageWriter writer, string propertyType)
    {
        if (codeElement.Parent is not CodeClass parentClass) throw new InvalidOperationException("The parent of a property should be a class");
        var backingStoreProperty = parentClass.GetBackingStoreProperty();
        var setterAccessModifier = codeElement.ReadOnly && codeElement.Access > AccessModifier.Private ? "private " : string.Empty;
        var defaultValue = string.Empty;
        if (!string.IsNullOrEmpty(codeElement.DefaultValue))
        {
            defaultValue = $" = {codeElement.DefaultValue}";
        }
        switch (codeElement.Kind)
        {
            case CodePropertyKind.RequestBuilder:
                writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} property {codeElement.Name.ToFirstCharacterLowerCase()} : {propertyType}");
                writer.WriteLine("{");
                writer.IncreaseIndent();
                writer.Write("getter ");
                conventions.AddRequestBuilderBody(parentClass, propertyType, writer, includeIndent: false);
                writer.DecreaseIndent();
                writer.WriteLine("}");
                break;
            case CodePropertyKind.AdditionalData when backingStoreProperty != null:
            case CodePropertyKind.Custom when backingStoreProperty != null:
                var backingStoreKey = codeElement.WireName;
                var nullableOp = !codeElement.IsOfKind(CodePropertyKind.AdditionalData) ? "?" : string.Empty;
                var defaultPropertyValue = codeElement.IsOfKind(CodePropertyKind.AdditionalData) ? " || Hash(String, Object).new" : string.Empty;
                writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} property {codeElement.Name.ToFirstCharacterLowerCase()} : {propertyType}");
                writer.WriteLine("{");
                writer.IncreaseIndent();
                writer.WriteLine($"getter {{ @backing_store{nullableOp}[\"{backingStoreKey}\"]{defaultPropertyValue} }}");
                writer.WriteLine($"setter {{ @backing_store{nullableOp}[\"{backingStoreKey}\"] = value }}");
                writer.DecreaseIndent();
                writer.WriteLine("}");
                break;
            case CodePropertyKind.ErrorMessageOverride when parentClass.IsErrorDefinition:
                if (parentClass.GetPrimaryMessageCodePath(static x => x.Name.ToFirstCharacterLowerCase(), static x => x.Name.ToFirstCharacterLowerCase(), "?.") is string primaryMessageCodePath && !string.IsNullOrEmpty(primaryMessageCodePath))
                    writer.WriteLine($"property {codeElement.Name.ToFirstCharacterLowerCase()} : {propertyType} {{ getter {{ {primaryMessageCodePath} || \"\" }} }}");
                else
                    writer.WriteLine($"property {codeElement.Name.ToFirstCharacterLowerCase()} : {propertyType} {{ getter {{ super.message }} }}");
                break;
            case CodePropertyKind.QueryParameter when codeElement.IsNameEscaped:
                writer.WriteLine($"@[QueryParameter(\"{codeElement.SerializationName}\")]");
                goto default;
            case CodePropertyKind.QueryParameters:
                defaultValue = $" = {propertyType}.new";
                goto default;
            default:
                if (!string.IsNullOrEmpty(codeElement.DefaultValue))
                {
                    writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} property {codeElement.Name.ToFirstCharacterLowerCase()} : {propertyType}");
                    writer.WriteLine("{");
                    writer.IncreaseIndent();
                    writer.WriteLine($"getter {{ {codeElement.DefaultValue} }}");
                    writer.DecreaseIndent();
                    writer.WriteLine("}");
                }
                else if (propertyType.Contains("Hash", StringComparison.OrdinalIgnoreCase) || propertyType.Contains("Dictionary", StringComparison.OrdinalIgnoreCase))
                {
                    writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} property {codeElement.Name.ToFirstCharacterLowerCase()} : {propertyType}");
                    writer.WriteLine("{");
                    writer.IncreaseIndent();
                    writer.WriteLine($"getter {{ Hash(String, Object).new }}");
                    writer.DecreaseIndent();
                    writer.WriteLine("}");
                }
                else
                {
                    writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} property {codeElement.Name.ToFirstCharacterLowerCase()} : {propertyType}{defaultValue}");
                }
                break;
        }
    }
}
