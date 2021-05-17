using System;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Java {
    public class CodePropertyWriter : BaseElementWriter<CodeProperty, JavaConventionService>
    {
        public CodePropertyWriter(JavaConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
        {
            conventions.WriteShortDescription(codeElement.Description, writer);
            var returnType = conventions.GetTypeString(codeElement.Type);
            switch(codeElement.PropertyKind) {
                case CodePropertyKind.RequestBuilder:
                    writer.WriteLine("@javax.annotation.Nonnull");
                    writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} {returnType} {codeElement.Name.ToFirstCharacterLowerCase()}() {{");
                    writer.IncreaseIndent();
                    conventions.AddRequestBuilderBody(returnType, writer);
                    writer.DecreaseIndent();
                    writer.WriteLine("}");
                break;
                case CodePropertyKind.Deserializer:
                    throw new InvalidOperationException("java uses methods for the deserializer and this property should have been converted by the refiner");
                default:
                    var defaultValue = string.IsNullOrEmpty(codeElement.DefaultValue) ? string.Empty : $" = {codeElement.DefaultValue}";
                    if(codeElement.Type is CodeType currentType && currentType.TypeDefinition is CodeEnum enumType && enumType.Flags)
                        returnType = $"EnumSet<{returnType}>";
                    writer.WriteLine(codeElement.Type.IsNullable ? "@javax.annotation.Nullable" : "@javax.annotation.Nonnull");
                    writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)}{(codeElement.ReadOnly ? " final " : " ")}{returnType} {codeElement.Name.ToFirstCharacterLowerCase()}{defaultValue};");
                break;
            }

        }
    }
}
