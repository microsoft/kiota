using System;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Java {
    public class CodePropertyWriter : BaseElementWriter<CodeProperty, JavaConventionService>
    {
        public CodePropertyWriter(JavaConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
        {
            conventions.WriteShortDescription(codeElement.Description, writer);
            var returnType = conventions.GetTypeString(codeElement.Type, codeElement);
            var parentClass = codeElement.Parent as CodeClass;
            switch(codeElement.Kind) {
                case CodePropertyKind.RequestBuilder:
                    writer.WriteLine("@javax.annotation.Nonnull");
                    writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} {returnType} {codeElement.Name.ToFirstCharacterLowerCase()}() {{");
                    writer.IncreaseIndent();
                    conventions.AddRequestBuilderBody(parentClass, returnType, writer);
                    writer.DecreaseIndent();
                    writer.WriteLine("}");
                break;
                default:
                    if(codeElement.Type is CodeType currentType && currentType.TypeDefinition is CodeEnum enumType && enumType.Flags)
                        returnType = $"EnumSet<{returnType}>";
                    if(codeElement.Access != AccessModifier.Private)
                        writer.WriteLine(codeElement.Type.IsNullable ? "@javax.annotation.Nullable" : "@javax.annotation.Nonnull");
                    writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)}{(codeElement.ReadOnly ? " final " : " ")}{returnType} {codeElement.NamePrefix}{codeElement.Name.ToFirstCharacterLowerCase()};");
                break;
            }

        }
    }
}
