using System.Diagnostics;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Php
{
    public class CodePropertyWriter: BaseElementWriter<CodeProperty, PhpConventionService>
    {
        public CodePropertyWriter(PhpConventionService conventionService) : base(conventionService) { }

        public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
        {
            
            var returnType = conventions.GetTypeString(codeElement.Type, codeElement);
            var currentPathProperty = codeElement.Parent.GetChildElements(true)
                .OfType<CodeProperty>()
                .FirstOrDefault(x => x.IsOfKind(CodePropertyKind.PathParameters));
            var propertyName = codeElement.Name.ToFirstCharacterLowerCase();
            var propertyAccess = conventions.GetAccessModifier(codeElement.Access);
            switch (codeElement.PropertyKind)
            {
                case CodePropertyKind.RequestBuilder:
                    conventions.WriteShortDescription(codeElement.Description, writer);
                    writer.WriteLine($"{propertyAccess} function {propertyName}(): {returnType} {{");
                    writer.IncreaseIndent();
                    conventions.AddRequestBuilderBody(currentPathProperty != null, returnType, writer);
                    writer.DecreaseIndent();
                    writer.WriteLine("}");
                    break;
                case CodePropertyKind.RequestAdapter:
                    WritePropertyDocComment(codeElement, writer);
                    writer.WriteLine($"{propertyAccess} RequestAdapter ${propertyName};");
                    break;
                case CodePropertyKind.AdditionalData or CodePropertyKind.PathParameters:
                    WritePropertyDocComment(codeElement, writer);
                    writer.WriteLine($"{propertyAccess} array ${propertyName};");
                    break;
                default:
                    WritePropertyDocComment(codeElement, writer);
                    writer.WriteLine($"{propertyAccess} {returnType} ${propertyName};");
                    break;
            }
            writer.WriteLine("");
        }

        private void WritePropertyDocComment(CodeProperty codeProperty, LanguageWriter writer)
        {
            var propertyDescription = codeProperty.Description;
            var hasDescription = !string.IsNullOrEmpty(codeProperty.Description);
            writer.WriteLine($"{conventions.DocCommentStart} @var {conventions.GetTypeString(codeProperty.Type, codeProperty)} ${codeProperty.Name} " +
                             $"{(hasDescription ? propertyDescription : string.Empty)} {conventions.DocCommentEnd}");
        }
    }
}
