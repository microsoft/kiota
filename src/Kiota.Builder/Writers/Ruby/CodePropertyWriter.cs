using System;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Ruby {
    public class CodePropertyWriter : BaseElementWriter<CodeProperty, RubyConventionService>
    {
        public CodePropertyWriter(RubyConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
        {
            conventions.WriteShortDescription(codeElement.Description, writer);
            switch(codeElement.PropertyKind) {
                case CodePropertyKind.RequestBuilder:
                    writer.WriteLine($"def {codeElement.Name.ToSnakeCase()}()");
                    writer.IncreaseIndent();
                    conventions.AddRequestBuilderBody(writer);
                    writer.DecreaseIndent();
                    writer.WriteLine("end");
                break;
                default:
                    writer.WriteLine($"@{codeElement.NamePrefix}{codeElement.Name.ToSnakeCase()}");
                break;
            }
        }
    }
}
