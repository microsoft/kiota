using System;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Ruby;

public class CodePropertyWriter : BaseElementWriter<CodeProperty, RubyConventionService>
{
    public CodePropertyWriter(RubyConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.ExistsInExternalBaseType) return;
        conventions.WriteShortDescription(codeElement, writer);
        var returnType = conventions.GetTypeString(codeElement.Type, codeElement);
        if (codeElement.Parent is not CodeClass parentClass) throw new InvalidOperationException("The parent of a property should be a class");
        switch (codeElement.Kind)
        {
            case CodePropertyKind.RequestBuilder:
                writer.WriteLine($"def {codeElement.Name.ToSnakeCase()}()");
                writer.IncreaseIndent();
                var prefix = conventions.GetNormalizedNamespacePrefixForType(codeElement.Type);
                conventions.AddRequestBuilderBody(parentClass, returnType, writer, prefix: $"return {prefix}");
                writer.DecreaseIndent();
                writer.WriteLine("end");
                break;
            case CodePropertyKind.QueryParameter:
            case CodePropertyKind.QueryParameters:
            case CodePropertyKind.Headers:
            case CodePropertyKind.Options:
                writer.WriteLine($"attr_accessor :{codeElement.Name.ToSnakeCase()}");
                break;
            default:
                writer.WriteLine($"@{codeElement.NamePrefix}{codeElement.Name.ToSnakeCase()}");
                break;
        }
    }
}
