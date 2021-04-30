using System;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.CSharp {
    public class CodePropertyWriter : BaseCSharpElementWriter<CodeProperty>
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
                case CodePropertyKind.Deserializer:
                    var parentClass = codeElement.Parent as CodeClass;
                    var hideParentMember = (parentClass.StartBlock as CodeClass.Declaration).Inherits != null;
                    writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} {(hideParentMember ? "new " : string.Empty)}{codeElement.Type.Name} {codeElement.Name.ToFirstCharacterUpperCase()} => new Dictionary<string, Action<{parentClass.Name.ToFirstCharacterUpperCase()}, {conventions.ParseNodeInterfaceName}>> {{");
                    writer.IncreaseIndent();
                    foreach(var otherProp in parentClass
                                                    .GetChildElements(true)
                                                    .OfType<CodeProperty>()
                                                    .Where(x => x.PropertyKind == CodePropertyKind.Custom)
                                                    .OrderBy(x => x.Name)) {
                        writer.WriteLine("{");
                        writer.IncreaseIndent();
                        writer.WriteLine($"\"{otherProp.SerializationName ?? otherProp.Name.ToFirstCharacterLowerCase()}\", (o,n) => {{ o.{otherProp.Name.ToFirstCharacterUpperCase()} = n.{GetDeserializationMethodName(otherProp.Type)}(); }}");
                        writer.DecreaseIndent();
                        writer.WriteLine("},");
                    }
                    writer.DecreaseIndent();
                    writer.WriteLine("};");
                break;
                default:
                    writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} {propertyType} {codeElement.Name.ToFirstCharacterUpperCase()} {{ {simpleBody} }}{defaultValue}");
                break;
            }
        }
        private string GetDeserializationMethodName(CodeTypeBase propType) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = conventions.TranslateType(propType.Name);
            if(propType is CodeType currentType) {
                if(isCollection)
                    if(currentType.TypeDefinition == null)
                        return $"GetCollectionOfPrimitiveValues<{propertyType}>().ToList";
                    else
                        return $"GetCollectionOfObjectValues<{propertyType}>().ToList";
                else if (currentType.TypeDefinition is CodeEnum enumType)
                    return $"GetEnumValue<{enumType.Name.ToFirstCharacterUpperCase()}>";
            }
            switch(propertyType) {
                case "string":
                case "bool":
                case "int":
                case "float":
                case "double":
                case "Guid":
                case "DateTimeOffset":
                    return $"Get{propertyType.ToFirstCharacterUpperCase()}Value";
                default:
                    return $"GetObjectValue<{propertyType.ToFirstCharacterUpperCase()}>";
            }
        }
    }
}
