using Kiota.Builder.CodeDOM;
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
            var propertyDescription = codeProperty.Documentation.Description;
            var hasDescription = !string.IsNullOrEmpty(propertyDescription);

            var collectionKind = codeProperty.Type.IsArray || codeProperty.Type.IsCollection;
            var typeString = (collectionKind
                ? GetCollectionDocString(codeProperty)
                : conventions.GetTypeString(codeProperty.Type, codeProperty));
            writer.WriteLine(PhpConventionService.DocCommentStart);
            if (codeProperty.IsOfKind(CodePropertyKind.QueryParameter) && codeProperty.IsNameEscaped)
            {
                writer.WriteLine($"{conventions.DocCommentPrefix}@QueryParameter(\"{codeProperty.SerializationName}\")");
            }
            writer.WriteLine($"{conventions.DocCommentPrefix}@var {typeString}{(codeProperty.Type.IsNullable ? "|null" : string.Empty)} ${codeProperty.Name.ToFirstCharacterLowerCase()} " +
                             $"{(hasDescription ? propertyDescription : string.Empty)}");
            writer.WriteLine(PhpConventionService.DocCommentEnd);
        }

        private string GetCollectionDocString(CodeProperty codeProperty)
        {
            return codeProperty.Kind switch
            {
                CodePropertyKind.AdditionalData => "array<string, mixed>",
                CodePropertyKind.PathParameters => "array<string, mixed>",
                CodePropertyKind.Headers => "array<string, array<string>|string>",
                CodePropertyKind.Options => "array<string, RequestOption>",
                _ => $"array<{conventions.TranslateType(codeProperty.Type)}>"
            };
        }

        private void WriteRequestBuilderBody(CodeProperty codeElement, LanguageWriter writer, string returnType, string propertyAccess, string propertyName)
        {
            conventions.WriteShortDescription(codeElement.Documentation.Description, writer);
            writer.WriteLine($"{propertyAccess} function {propertyName}(): {returnType} {{");
            writer.IncreaseIndent();
            conventions.AddRequestBuilderBody(returnType, writer);
            writer.CloseBlock();
        }
    }
}
