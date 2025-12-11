using System;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Go;

public class CodePropertyWriter : BaseElementWriter<CodeProperty, GoConventionService>
{
    public CodePropertyWriter(GoConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.ExistsInExternalBaseType)
            return;
        var propertyName = codeElement.Access == AccessModifier.Public ? codeElement.Name.ToFirstCharacterUpperCase() : codeElement.Name.ToFirstCharacterLowerCase();
        var suffix = string.Empty;
        switch (codeElement.Kind)
        {
            case CodePropertyKind.RequestBuilder:
                throw new InvalidOperationException("RequestBuilders are as properties are not supported in Go and should be replaced by methods by the refiner.");
            case CodePropertyKind.ErrorMessageOverride:
                throw new InvalidOperationException("Error message overrides are implemented with methods in Go.");
            case CodePropertyKind.QueryParameter when codeElement.IsNameEscaped:
                suffix = $" `uriparametername:\"{codeElement.SerializationName}\"`";
                goto default;
            default:
                var returnType = codeElement.Parent is CodeElement parent ? conventions.GetTypeString(codeElement.Type, parent) : string.Empty;
                conventions.WriteShortDescription(codeElement, writer);
                conventions.WriteDeprecation(codeElement, writer);
                writer.WriteLine($"{propertyName} {returnType}{suffix}");
                break;
        }
    }
}
