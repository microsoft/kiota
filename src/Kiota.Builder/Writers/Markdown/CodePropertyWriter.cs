using System;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Markdown;
public class CodePropertyWriter : BaseElementWriter<CodeProperty, MarkdownConventionService>
{
    public CodePropertyWriter(MarkdownConventionService conventionService): base(conventionService) { }
    public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
    {
        var propertyType = conventions.GetTypeString(codeElement.Type, codeElement);
        var defaultValue = string.Empty;
        switch(codeElement.Kind) {
            case CodePropertyKind.RequestBuilder:
                writer.WriteLine($"|[{codeElement.Name}]({codeElement.Name}/{codeElement.Name}RequestBuilder.md) | {codeElement.Documentation?.Description ?? ""} |");
            break;
            case CodePropertyKind.QueryParameter when codeElement.IsNameEscaped:
                writer.WriteLine($"[QueryParameter(\"{codeElement.SerializationName}\")]");
                goto default;
            case CodePropertyKind.QueryParameters:
                defaultValue = $" = new {propertyType}();";
                goto default;
            default:
                if (codeElement.Parent is CodeClass parentClass && parentClass.Kind != CodeClassKind.RequestConfiguration) {
                    writer.WriteLine($"| {codeElement.Name} | {propertyType} | {defaultValue}| {codeElement.Documentation?.Description ?? ""} |");
                }
            break;
        }
    }
}
