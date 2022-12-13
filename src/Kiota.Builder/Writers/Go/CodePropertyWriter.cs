using System;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Go {
    public class CodePropertyWriter : BaseElementWriter<CodeProperty, GoConventionService>
    {
        public CodePropertyWriter(GoConventionService conventionService) : base(conventionService) {}
        public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
        {
            var propertyName = codeElement.Access == AccessModifier.Public ? codeElement.Name.ToFirstCharacterUpperCase() : codeElement.Name.ToFirstCharacterLowerCase();
            var returnType = conventions.GetTypeString(codeElement.Type, codeElement.Parent);
            var suffix = string.Empty;
            switch(codeElement.Kind) {
                case CodePropertyKind.RequestBuilder:
                    throw new InvalidOperationException("RequestBuilders are as properties are not supported in Go and should be replaced by methods by the refiner.");
                case CodePropertyKind.QueryParameter when codeElement.IsNameEscaped:
                    suffix = $" `uriparametername:\"{codeElement.SerializationName}\"`";
                    goto default;
                default:
                    conventions.WriteShortDescription(codeElement.Documentation.Description, writer);
                    writer.WriteLine($"{propertyName} {returnType}{suffix}");
                break;
            }
        }
    }
}
