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
        if (codeElement.Parent is not CodeClass parentClass) throw new InvalidOperationException("The parent of a property should be a class");
        var primaryMessageCodePath = codeElement.Kind is CodePropertyKind.ErrorMessageOverride && parentClass.IsErrorDefinition ?
            parentClass.GetPrimaryMessageCodePath(static x => x.Name.ToSnakeCase(), static x => x.Name.ToSnakeCase(), "&.") :
            string.Empty;
        // without a primary message the inherited StandardError#message already applies
        if (codeElement.Kind is CodePropertyKind.ErrorMessageOverride && string.IsNullOrEmpty(primaryMessageCodePath)) return;
        RubyConventionService.WriteMemberSeparator(writer);
        conventions.WriteShortDescription(codeElement, writer);
        switch (codeElement.Kind)
        {
            case CodePropertyKind.ErrorMessageOverride:
                writer.WriteLine($"def {codeElement.Name.ToSnakeCase()}");
                writer.IncreaseIndent();
                writer.WriteLine($"{primaryMessageCodePath} || \"\"");
                writer.DecreaseIndent();
                writer.WriteLine("end");
                break;
            case CodePropertyKind.RequestBuilder:
                writer.WriteLine($"def {codeElement.Name.ToSnakeCase()}");
                writer.IncreaseIndent();
                conventions.AddRequestBuilderBody(parentClass, conventions.GetQualifiedTypeName(codeElement.Type), writer);
                writer.DecreaseIndent();
                writer.WriteLine("end");
                break;
            case CodePropertyKind.QueryParameter:
            case CodePropertyKind.QueryParameters:
            case CodePropertyKind.Headers:
            case CodePropertyKind.Options:
            case CodePropertyKind.Custom:
            case CodePropertyKind.AdditionalData:
            case CodePropertyKind.BackingStore:
                writer.WriteLine($"attr_accessor :{codeElement.Name.ToSnakeCase()}");
                break;
            default:
                writer.WriteLine($"@{codeElement.NamePrefix}{codeElement.Name.ToSnakeCase()}");
                break;
        }
    }
}
