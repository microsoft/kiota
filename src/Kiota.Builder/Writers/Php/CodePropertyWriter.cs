using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Php
{
    public class CodePropertyWriter: BaseElementWriter<CodeProperty, PhpConventionService>
    {
        public CodePropertyWriter(PhpConventionService conventionService) : base(conventionService) { }

        public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
        {
            
            var returnType = conventions.GetTypeString(codeElement.Type, codeElement);
            var propertyName = codeElement.Name.ToFirstCharacterLowerCase();
            var propertyAccess = conventions.GetAccessModifier(codeElement.Access);
            switch (codeElement.Kind)
            {
                case CodePropertyKind.RequestBuilder:
                    WriteRequestBuilderBody(codeElement, writer, returnType, propertyAccess, propertyName);
                    break;
                default:
                    WritePropertyDocComment(codeElement, writer);
                    writer.WriteLine($"{propertyAccess} {(codeElement.Type.IsNullable ? "?" : string.Empty)}{returnType} ${propertyName}{(codeElement.Type.IsNullable ? " = null" : string.Empty)};");
                    break;
            }
            writer.WriteLine("");
        }

        private void WritePropertyDocComment(CodeProperty codeProperty, LanguageWriter writer)
        {
            var propertyDescription = codeProperty.Description;
            var hasDescription = !string.IsNullOrEmpty(propertyDescription);

            var collectionKind = codeProperty.Type.IsArray || codeProperty.Type.IsCollection;
            var typeString = (collectionKind
                ? GetCollectionDocString(codeProperty)
                : conventions.GetTypeString(codeProperty.Type, codeProperty));
            writer.WriteLine($"{conventions.DocCommentStart} @var {typeString}{(codeProperty.Type.IsNullable ? "|null" : string.Empty)} ${codeProperty.Name} " +
                             $"{(hasDescription ? propertyDescription : string.Empty)} {conventions.DocCommentEnd}");
        }

        private string GetCollectionDocString(CodeProperty codeProperty)
        {
            return codeProperty.IsOfKind(CodePropertyKind.AdditionalData, CodePropertyKind.PathParameters) ? "array<string, mixed>" : $"array<{conventions.TranslateType(codeProperty.Type)}>";
        }

        private void WriteRequestBuilderBody(CodeProperty codeElement, LanguageWriter writer, string returnType, string propertyAccess, string propertyName)
        {
            conventions.WriteShortDescription(codeElement.Description, writer);
            writer.WriteLine($"{propertyAccess} function {propertyName}(): {returnType} {{");
            writer.IncreaseIndent();
            conventions.AddRequestBuilderBody(returnType, writer);
            writer.CloseBlock();
        }
    }
}
