using System;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Extensions;

namespace Kiota.Builder.Writers.CSharp;
public class CodePropertyWriter : BaseElementWriter<CodeProperty, CSharpConventionService>
{
    public CodePropertyWriter(CSharpConventionService conventionService): base(conventionService) { }
    public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
    {
        var parentClass = codeElement.Parent as CodeClass;
        var backingStoreProperty = parentClass.GetBackingStoreProperty();
        var setterAccessModifier = codeElement.ReadOnly && codeElement.Access > AccessModifier.Private ? "private " : string.Empty;
        var simpleBody = $"get; {setterAccessModifier}set;";
        var propertyType = conventions.GetTypeString(codeElement.Type, codeElement);
        var defaultValue = string.Empty;
        conventions.WriteShortDescription(codeElement.Description, writer);
        switch(codeElement.Kind) {
            case CodePropertyKind.RequestBuilder:
                writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} {propertyType} {codeElement.Name.ToFirstCharacterUpperCase()} {{ get =>");
                writer.IncreaseIndent();
                conventions.AddRequestBuilderBody(parentClass, propertyType, writer);
                writer.DecreaseIndent();
                writer.WriteLine("}");
            break;
            case CodePropertyKind.AdditionalData when backingStoreProperty != null:
            case CodePropertyKind.Custom when backingStoreProperty != null:
                writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} {propertyType} {codeElement.Name.ToFirstCharacterUpperCase()} {{");
                writer.IncreaseIndent();
                writer.WriteLine($"get {{ return {backingStoreProperty.Name.ToFirstCharacterUpperCase()}?.Get<{propertyType}>(nameof({codeElement.Name.ToFirstCharacterUpperCase()})); }}");
                writer.WriteLine($"set {{ {backingStoreProperty.Name.ToFirstCharacterUpperCase()}?.Set(nameof({codeElement.Name.ToFirstCharacterUpperCase()}), value); }}");
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
