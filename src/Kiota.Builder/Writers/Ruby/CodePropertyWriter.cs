using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Ruby {
    public class CodePropertyWriter : BaseElementWriter<CodeProperty, RubyConventionService>
    {
        public CodePropertyWriter(RubyConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
        {
            conventions.WriteShortDescription(codeElement.Documentation.Description, writer);
            var returnType = conventions.GetTypeString(codeElement.Type, codeElement);
            var parentClass = codeElement.Parent as CodeClass;
            switch(codeElement.Kind) {
                case CodePropertyKind.RequestBuilder:
                    writer.WriteLine($"def {codeElement.Name.ToSnakeCase()}()");
                    writer.IncreaseIndent();
                    var prefix = conventions.GetNormalizedNamespacePrefixForType(codeElement.Type);
                    conventions.AddRequestBuilderBody(parentClass, returnType, writer, prefix: $"return {prefix}");
                    writer.DecreaseIndent();
                    writer.WriteLine("end");
                break;
                default:
                    writer.WriteLine($"@{codeElement.Name.ToSnakeCase()}");
                break;
            }
        }
    }
}
