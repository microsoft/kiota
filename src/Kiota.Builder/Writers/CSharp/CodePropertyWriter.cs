using System;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Extensions;

namespace Kiota.Builder.Writers.CSharp {
    public class CodePropertyWriter : BaseElementWriter<CodeProperty, CSharpConventionService>
    {
        public CodePropertyWriter(CSharpConventionService conventionService): base(conventionService) { }
        public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
        {
            var parentClass = codeElement.Parent as CodeClass;
            var backingStorePropery = parentClass.GetBackingStoreProperty();
            var setterAccessModifier = codeElement.ReadOnly && codeElement.Access > AccessModifier.Private ? "private " : string.Empty;
            var simpleBody = $"get; {setterAccessModifier}set;";
            var propertyType = conventions.GetTypeString(codeElement.Type, codeElement);
            conventions.WriteShortDescription(codeElement.Description, writer);
            switch(codeElement.PropertyKind) {
                case CodePropertyKind.RequestBuilder:
                    var currentPathProperty = parentClass.Properties.FirstOrDefault(x => x.IsOfKind(CodePropertyKind.CurrentPath));
                    writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} {propertyType} {codeElement.Name.ToFirstCharacterUpperCase()} {{ get =>");
                    writer.IncreaseIndent();
                    conventions.AddRequestBuilderBody(currentPathProperty != null, propertyType, writer);
                    writer.DecreaseIndent();
                    writer.WriteLine("}");
                break;
                case CodePropertyKind.AdditionalData when backingStorePropery != null:
                case CodePropertyKind.Custom when backingStorePropery != null:
                    writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} {propertyType} {codeElement.Name.ToFirstCharacterUpperCase()} {{");
                    writer.IncreaseIndent();
                    writer.WriteLine($"get {{ return {backingStorePropery.Name.ToFirstCharacterUpperCase()}?.Get<{propertyType}>(nameof({codeElement.Name.ToFirstCharacterUpperCase()})); }}");
                    writer.WriteLine($"set {{ {backingStorePropery.Name.ToFirstCharacterUpperCase()}?.Set(nameof({codeElement.Name.ToFirstCharacterUpperCase()}), value); }}");
                    writer.DecreaseIndent();
                    writer.WriteLine("}");
                break;
                default:
                    writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} {propertyType} {codeElement.Name.ToFirstCharacterUpperCase()} {{ {simpleBody} }}");
                break;
            }
        }
        
    }
}
