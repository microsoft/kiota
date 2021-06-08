using System;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.CSharp {
    public class CodePropertyWriter : BaseElementWriter<CodeProperty, CSharpConventionService>
    {
        public CodePropertyWriter(CSharpConventionService conventionService): base(conventionService) { }
        public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
        {
            var setterAccessModifier = codeElement.ReadOnly && codeElement.Access > AccessModifier.Private ? "private " : string.Empty;
            var simpleBody = $"get; {setterAccessModifier}set;";
            var defaultValue = string.Empty;
            if (codeElement.DefaultValue != null)
            {
                defaultValue = " = " + codeElement.DefaultValue + ";";
            }
            var propertyType = conventions.GetTypeString(codeElement.Type);
            conventions.WriteShortDescription(codeElement.Description, writer);
            switch(codeElement.PropertyKind) {
                case CodePropertyKind.RequestBuilder:
                    writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} {propertyType} {codeElement.Name.ToFirstCharacterUpperCase()} {{ get =>");
                    writer.IncreaseIndent();
                    conventions.AddRequestBuilderBody(propertyType, writer);
                    writer.DecreaseIndent();
                    writer.WriteLine("}");
                break;
                default:
                    writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} {propertyType} {codeElement.Name.ToFirstCharacterUpperCase()} {{ {simpleBody} }}{defaultValue}");
                break;
            }
        }
        
    }
}
