using System;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.CSharp {
    public class CodePropertyWriter : BaseElementWriter<CodeProperty, CSharpConventionService>
    {
        public CodePropertyWriter(CSharpConventionService conventionService): base(conventionService) { }
        public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
        {
            var parentClass = codeElement.Parent as CodeClass;
            var backingStorePropery = (parentClass.GetGreatestGrandparent(parentClass) ?? parentClass) // the backing store is always on the uppermost class
                                    .GetChildElements(true)
                                    .OfType<CodeProperty>()
                                    .FirstOrDefault(x => x.PropertyKind == CodePropertyKind.BackingStore);
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
                    writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} {propertyType} {codeElement.Name.ToFirstCharacterUpperCase()} {{ {simpleBody} }}{defaultValue}");
                break;
            }
        }
        
    }
}
