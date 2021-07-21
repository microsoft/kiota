using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Ruby {
    public class CodePropertyWriter : BaseElementWriter<CodeProperty, RubyConventionService>
    {
        public CodePropertyWriter(RubyConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
        {
            var accessModifier = conventions.GetAccessModifier(codeElement.Access);
            conventions.WriteShortDescription(codeElement.Description, writer);
            var returnType = conventions.GetTypeString(codeElement.Type);
            switch(codeElement.PropertyKind) {
                case CodePropertyKind.RequestBuilder:
                    writer.WriteLine($"def {codeElement.Name.ToSnakeCase()}()");
                    writer.IncreaseIndent();
                    conventions.AddRequestBuilderBody(returnType, writer);
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
