using System;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Dart;
public class CodePropertyWriter : BaseElementWriter<CodeProperty, DartConventionService>
{
    public CodePropertyWriter(DartConventionService conventionService) : base(conventionService) { }
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
        conventions.WriteShortDescription(codeElement.Documentation.Description, writer);
        conventions.WriteDeprecationAttribute(codeElement, writer);
        if (isNullableReferenceType)
        {
            WritePropertyInternal(codeElement, writer, $"{propertyType}?");
        }

        WritePropertyInternal(codeElement, writer, propertyType);// Always write the normal way
    }

    private void WritePropertyInternal(CodeProperty codeElement, LanguageWriter writer, string propertyType)
    {
        if (codeElement.Parent is not CodeClass parentClass)
            throw new InvalidOperationException("The parent of a property should be a class");

        var backingStoreProperty = parentClass.GetBackingStoreProperty();
        var setterAccessModifier = codeElement.ReadOnly && codeElement.Access > AccessModifier.Private ? "_" : string.Empty;
        var defaultValue = string.Empty;
        var getterModifier = string.Empty;

        var accessModifierAttribute = conventions.GetAccessModifierAttribute(codeElement.Access);
        if (!string.IsNullOrEmpty(accessModifierAttribute))
            writer.WriteLine(accessModifierAttribute);

        switch (codeElement.Kind)
        {
            case CodePropertyKind.RequestBuilder:
                writer.WriteLine($"{propertyType} get {conventions.GetAccessModifierPrefix(codeElement.Access)}{codeElement.Name.ToCamelCase()} {{");
                writer.IncreaseIndent();
                conventions.AddRequestBuilderBody(parentClass, propertyType, writer, prefix: "return ");
                writer.DecreaseIndent();
                writer.WriteLine("}");
                break;
            case CodePropertyKind.AdditionalData when backingStoreProperty != null:
            case CodePropertyKind.Custom when backingStoreProperty != null:
                var backingStoreKey = codeElement.WireName;
                writer.WriteLine($"{propertyType} get {conventions.GetAccessModifierPrefix(codeElement.Access)}{codeElement.Name.ToCamelCase()} {{");
                writer.IncreaseIndent();
                writer.WriteLine($"return {backingStoreProperty.Name.ToFirstCharacterUpperCase()}?.Get<{propertyType}>(\"{backingStoreKey}\");");
                writer.DecreaseIndent();
                writer.WriteLine("}");
                writer.WriteLine();
                writer.WriteLine($"set {setterAccessModifier}{codeElement.Name.ToCamelCase()}({propertyType} value) {{");
                writer.IncreaseIndent();
                writer.WriteLine($"{backingStoreProperty.Name.ToFirstCharacterUpperCase()}?.Set(\"{backingStoreKey}\", value);");
                writer.DecreaseIndent();
                writer.WriteLine("}");
                break;
            case CodePropertyKind.ErrorMessageOverride when parentClass.IsErrorDefinition:
                writer.WriteLine("@override");

                if (parentClass.GetPrimaryMessageCodePath(static x => x.Name.ToFirstCharacterUpperCase(),
                        static x => x.Name.ToFirstCharacterUpperCase(), "?.") is { } primaryMessageCodePath &&
                    !string.IsNullOrEmpty(primaryMessageCodePath))
                    defaultValue = $"=> {primaryMessageCodePath} ?? \"\";";
                else
                    defaultValue = "=> super.Message;";

                getterModifier = "get ";
                goto default;
            case CodePropertyKind.QueryParameter when codeElement.IsNameEscaped:
                writer.WriteLine($"/// @QueryParameter(\"{codeElement.SerializationName}\")");
                goto default;
            case CodePropertyKind.QueryParameters:
                defaultValue = $" = {propertyType}()";
                goto default;
            default:
                writer.WriteLine($"{propertyType} {getterModifier}{conventions.GetAccessModifierPrefix(codeElement.Access)}{codeElement.Name.ToCamelCase()}{defaultValue};");
                break;
        }
    }
}
