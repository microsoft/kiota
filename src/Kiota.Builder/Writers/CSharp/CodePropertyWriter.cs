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
        var isNullableReferenceType = !propertyType.EndsWith('?')
                                      && codeElement.IsOfKind(
                                            CodePropertyKind.Custom,
                                            CodePropertyKind.QueryParameter);// Other property types are appropriately constructor initialized
        conventions.WriteShortDescription(codeElement, writer);
        conventions.WriteDeprecationAttribute(codeElement, writer);
        conventions.WriteNullableAware(
            writer,
            isNullableReferenceType,
            writer => WritePropertyInternal(codeElement, writer, $"{propertyType}?"),
            writer => WritePropertyInternal(codeElement, writer, propertyType)
        );
    }

    private void WritePropertyInternal(CodeProperty codeElement, LanguageWriter writer, string propertyType)
    {
        if (codeElement.Parent is not CodeClass parentClass) throw new InvalidOperationException("The parent of a property should be a class");
        var backingStoreProperty = parentClass.GetBackingStoreProperty();
        var setterAccessModifier = codeElement.ReadOnly && codeElement.Access > AccessModifier.Private ? "private " : string.Empty;
        var simpleBody = $"get; {setterAccessModifier}set;";
        var defaultValue = string.Empty;
        var accessModifier = conventions.GetAccessModifier(codeElement.Access);
        var codeElementNameFirstCharacterUpperCase = codeElement.Name.ToFirstCharacterUpperCase();
        switch (codeElement.Kind)
        {
            case CodePropertyKind.RequestBuilder:
                writer.WriteLine($"{accessModifier} {propertyType} {codeElementNameFirstCharacterUpperCase}");
                writer.StartBlock();
                writer.Write("get => ");
                conventions.AddRequestBuilderBody(parentClass, propertyType, writer, includeIndent: false);
                writer.CloseBlock();
                break;
            case CodePropertyKind.AdditionalData when backingStoreProperty != null:
            case CodePropertyKind.Custom when backingStoreProperty != null:
                var backingStoreKey = codeElement.WireName;
                var nullableOp = !codeElement.IsOfKind(CodePropertyKind.AdditionalData) ? "?" : string.Empty;
                var defaultPropertyValue = codeElement.IsOfKind(CodePropertyKind.AdditionalData) ? " ?? new Dictionary<string, object>()" : string.Empty;
                writer.WriteLine($"{accessModifier} {propertyType} {codeElementNameFirstCharacterUpperCase}");
                writer.StartBlock();
                writer.WriteLine($"get {{ return {backingStoreProperty.Name.ToFirstCharacterUpperCase()}{nullableOp}.Get<{propertyType}>(\"{backingStoreKey}\"){defaultPropertyValue}; }}");
                writer.WriteLine($"set {{ {backingStoreProperty.Name.ToFirstCharacterUpperCase()}{nullableOp}.Set(\"{backingStoreKey}\", value); }}");
                writer.CloseBlock();
                break;
            case CodePropertyKind.ErrorMessageOverride when parentClass.IsErrorDefinition:
                if (parentClass.GetPrimaryMessageCodePath(static x => x.Name.ToFirstCharacterUpperCase(), static x => x.Name.ToFirstCharacterUpperCase(), "?.") is string primaryMessageCodePath && !string.IsNullOrEmpty(primaryMessageCodePath))
                    writer.WriteLine($"public override {propertyType} {codeElementNameFirstCharacterUpperCase} {{ get => {primaryMessageCodePath} ?? string.Empty; }}");
                else
                    writer.WriteLine($"public override {propertyType} {codeElementNameFirstCharacterUpperCase} {{ get => base.Message; }}");
                break;
            case CodePropertyKind.QueryParameter when codeElement.IsNameEscaped:
                writer.WriteLine($"[QueryParameter(\"{codeElement.SerializationName}\")]");
                goto default;
            case CodePropertyKind.QueryParameters:
                defaultValue = $" = new {propertyType}();";
                goto default;
            default:
                writer.WriteLine($"{accessModifier} {propertyType} {codeElementNameFirstCharacterUpperCase} {{ {simpleBody} }}{defaultValue}");
                break;
        }
    }
}
