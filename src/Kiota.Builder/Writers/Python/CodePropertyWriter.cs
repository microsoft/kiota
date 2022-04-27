using System;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Refiners;

namespace Kiota.Builder.Writers.Python {
    public class CodePropertyWriter : BaseElementWriter<CodeProperty, PythonConventionService>
    {
        public CodePropertyWriter(PythonConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
        {
            var returnType = conventions.GetTypeString(codeElement.Type, codeElement);
            var parentClass = codeElement.Parent as CodeClass;
            switch(codeElement.Kind) {
                case CodePropertyKind.RequestBuilder:
                    writer.WriteLine($"def get_{codeElement.Name.ToSnakeCase()}(self) -> {returnType}:");
                    writer.IncreaseIndent();
                    conventions.WriteShortDescription(codeElement.Description, writer);
                    conventions.AddRequestBuilderBody(parentClass, returnType, writer);
                    writer.DecreaseIndent();
                    writer.WriteLine();
                break;
                default:
                    conventions.WriteInLineDescription(codeElement.Description, writer);
                    writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)}{codeElement.NamePrefix}{codeElement.Name.ToSnakeCase()}: {(codeElement.Type.IsNullable ? "Optional[" : string.Empty)}{returnType}{(codeElement.Type.IsNullable ? "]" : string.Empty)} = None");
                    writer.WriteLine();
                break;
            }
        }
    }
}
