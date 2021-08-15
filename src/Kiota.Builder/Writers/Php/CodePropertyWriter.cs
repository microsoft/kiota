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
            conventions.WriteShortDescription(codeElement.Description, writer);

            var returnType = conventions.GetTypeString(codeElement.Type);
            var currentPathProperty = codeElement.Parent.GetChildElements(true)
                .OfType<CodeProperty>()
                .FirstOrDefault(x => x.IsOfKind(CodePropertyKind.CurrentPath));
            
            switch (codeElement.PropertyKind)
            {
                case CodePropertyKind.RequestBuilder:
                    writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} function {codeElement.Name.ToFirstCharacterLowerCase()}(): {returnType} {{");
                    writer.IncreaseIndent();
                    conventions.AddRequestBuilderBody(currentPathProperty != null, returnType, writer);
                    writer.DecreaseIndent();
                    writer.WriteLine("}");
                    break;
                default:
                    writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} {returnType} ${codeElement.Name.ToFirstCharacterLowerCase()};");
                    break;
            }
        }
    }
}
