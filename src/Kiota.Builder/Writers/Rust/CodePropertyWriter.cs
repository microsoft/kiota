using System;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Rust;

public class CodePropertyWriter : BaseElementWriter<CodeProperty, RustConventionService>
{
    public CodePropertyWriter(RustConventionService conventionService) : base(conventionService) { }

    public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.ExistsInExternalBaseType || conventions.ErrorClassPropertyExistsInSuperClass(codeElement)) return;

        var propertyType = conventions.GetTypeString(codeElement.Type, codeElement);
        var propertyName = codeElement.Name.ToSnakeCase();

        switch (codeElement.Kind)
        {
            case CodePropertyKind.RequestBuilder:
                // Request builder properties are accessed via methods in Rust, skip field declaration
                break;
            case CodePropertyKind.QueryParameter when codeElement.IsNameEscaped:
                conventions.WriteShortDescription(codeElement, writer);
                conventions.WriteDeprecationAttribute(codeElement, writer);
                WriteField(writer, propertyName, propertyType, codeElement);
                break;
            case CodePropertyKind.Custom when !string.IsNullOrEmpty(codeElement.SerializationName) && !codeElement.SerializationName.Equals(codeElement.Name, StringComparison.Ordinal):
                conventions.WriteShortDescription(codeElement, writer);
                conventions.WriteDeprecationAttribute(codeElement, writer);
                writer.WriteLine($"#[serde(rename = \"{codeElement.WireName}\")]");
                WriteField(writer, propertyName, propertyType, codeElement);
                break;
            case CodePropertyKind.AdditionalData:
                conventions.WriteShortDescription(codeElement, writer);
                conventions.WriteDeprecationAttribute(codeElement, writer);
                writer.WriteLine("#[serde(flatten)]");
                writer.WriteLine($"pub {propertyName}: std::collections::HashMap<String, serde_json::Value>,");
                break;
            case CodePropertyKind.BackingStore:
                // Backing store is not directly mapped to Rust
                break;
            default:
                conventions.WriteShortDescription(codeElement, writer);
                conventions.WriteDeprecationAttribute(codeElement, writer);
                WriteField(writer, propertyName, propertyType, codeElement);
                break;
        }
    }

    private static void WriteField(LanguageWriter writer, string propertyName, string propertyType, CodeProperty codeElement)
    {
        var isNullable = codeElement.Type.IsNullable && !propertyType.StartsWith("Option<", StringComparison.Ordinal);
        var finalType = isNullable ? $"Option<{propertyType}>" : propertyType;
        writer.WriteLine($"pub {propertyName}: {finalType},");
    }
}
