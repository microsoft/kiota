using System;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Java;

public class CodePropertyWriter : BaseElementWriter<CodeProperty, JavaConventionService>
{
    public CodePropertyWriter(JavaConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.ExistsInExternalBaseType)
            return;
        if (codeElement.Parent is not CodeClass parentClass) throw new InvalidOperationException("The parent of a property should be a class");
        var returnType = conventions.GetTypeString(codeElement.Type, codeElement);
        var returnRemark = codeElement.Kind is CodePropertyKind.RequestBuilder ?
                            conventions.GetReturnDocComment(returnType) :
                            string.Empty;
        conventions.WriteLongDescription(codeElement, writer, [returnRemark]);
        var defaultValue = string.Empty;
        conventions.WriteDeprecatedAnnotation(codeElement, writer);
        switch (codeElement.Kind)
        {
            case CodePropertyKind.ErrorMessageOverride:
                throw new InvalidOperationException("Error message overrides are implemented with methods in Java.");
            case CodePropertyKind.RequestBuilder:
                writer.WriteLine("@jakarta.annotation.Nonnull");
                writer.StartBlock($"{conventions.GetAccessModifier(codeElement.Access)} {returnType} {codeElement.Name}() {{");
                conventions.AddRequestBuilderBody(parentClass, returnType, writer);
                writer.CloseBlock();
                break;
            case CodePropertyKind.Headers or CodePropertyKind.Options when !string.IsNullOrEmpty(codeElement.DefaultValue):
                defaultValue = $" = {codeElement.DefaultValue}";
                goto default;
            case CodePropertyKind.QueryParameters:
                defaultValue = $" = new {returnType}()";
                goto default;
            default:
                if (codeElement.Type is CodeType { TypeDefinition: CodeEnum { Flags: true }, IsCollection: false })
                    returnType = $"EnumSet<{returnType}>";
                if (codeElement.Access != AccessModifier.Private)
                    writer.WriteLine(codeElement.Type.IsNullable ? "@jakarta.annotation.Nullable" : "@jakarta.annotation.Nonnull");
                writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} {returnType} {codeElement.NamePrefix}{codeElement.Name}{defaultValue};");
                break;
        }

    }
}
