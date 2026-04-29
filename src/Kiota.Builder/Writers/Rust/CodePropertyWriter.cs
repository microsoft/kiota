using System;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Rust;

public class CodePropertyWriter(RustConventionService conventionService) : BaseElementWriter<CodeProperty, RustConventionService>(conventionService)
{
    public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);

        if (codeElement.Kind == CodePropertyKind.RequestBuilder)
        {
            WriteNavigationProperty(codeElement, writer);
            return;
        }
        // Everything else is already written by CodeClassDeclarationWriter
    }

    private void WriteNavigationProperty(CodeProperty property, LanguageWriter writer)
    {
        // navigation properties return the builder directly, not Option<>
        var returnType = property.Type is CodeType ct ? ct.Name.ToFirstCharacterUpperCase() : conventions.GetTypeString(property.Type, property);
        var methodName = property.Name.ToSnakeCase();

        conventions.WriteShortDescription(property, writer);
        writer.WriteLine($"pub fn {methodName}(&self) -> {returnType} {{");
        writer.IncreaseIndent();
        writer.WriteLine($"{returnType}::new(self.base.path_parameters.clone(), self.base.request_adapter.clone())");
        writer.DecreaseIndent();
        writer.WriteLine("}");
    }
}
