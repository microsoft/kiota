using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Extensions;

namespace Kiota.Builder.Writers.Php
{
    public class CodeMethodWriter: BaseElementWriter<CodeMethod, PhpConventionService>
    {
        public CodeMethodWriter(PhpConventionService conventionService) : base(conventionService) { }

        public override void  WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
        {

            var parentClass = codeElement.Parent as CodeClass;
            var inherits = (parentClass?.StartBlock as CodeClass.Declaration)?.Inherits != null;
            var orNullReturn = codeElement.ReturnType.IsNullable ? new[]{"?", "|null"} : new[] {string.Empty, string.Empty};
            WriteMethodPhpDocs(codeElement, writer, orNullReturn);
            WriteMethodsAndParameters(codeElement, writer, orNullReturn, codeElement.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor));
            switch (codeElement.MethodKind)
            {
                    case CodeMethodKind.Constructor: 
                        WriteConstructorBody(parentClass, codeElement, writer, inherits);
                        break;
                    case CodeMethodKind.Serializer:
                        WriteSerializerBody(parentClass, writer, inherits);
                        break;
                    case CodeMethodKind.Setter:
                        WriteSetterBody(writer, codeElement);
                        break;
                    case CodeMethodKind.Getter:
                        WriteGetterBody(writer, codeElement);
                        break;
                    case CodeMethodKind.Deserializer:
                        WriteDeserializerBody(writer, codeElement);
                        break;
            }
            conventions.WriteCodeBlockEnd(writer);
            writer.WriteLine();
        }
        
        private void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer, bool inherits) {
            if(inherits)
                writer.WriteLine("parent::__construct();");
            foreach(var propWithDefault in parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData,
                                                                            CodePropertyKind.BackingStore,
                                                                            CodePropertyKind.RequestBuilder,
                                                                            CodePropertyKind.PathSegment)
                                            .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
                                            .OrderByDescending(x => x.PropertyKind)
                                            .ThenBy(x => x.Name)) {
                writer.WriteLine($"$this->{propWithDefault.NamePrefix}{propWithDefault.Name.ToFirstCharacterLowerCase()} = {conventions.ReplaceDoubleQuoteWithSingleQuote(propWithDefault.DefaultValue)};");
            }
            if(currentMethod.IsOfKind(CodeMethodKind.Constructor)) {
                AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.RequestAdapter, CodePropertyKind.RequestAdapter, writer);
                AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.CurrentPath, CodePropertyKind.CurrentPath, writer);
            }
        }
        private static void AssignPropertyFromParameter(CodeClass parentClass, CodeMethod currentMethod, CodeParameterKind parameterKind, CodePropertyKind propertyKind, LanguageWriter writer) {
            var property = parentClass.GetChildElements(true).OfType<CodeProperty>().FirstOrDefault(x => x.IsOfKind(propertyKind));
            var parameter = currentMethod.Parameters.FirstOrDefault(x => x.IsOfKind(parameterKind));
            if(property != null && parameter != null) {
                writer.WriteLine($"$this->{property.Name.ToFirstCharacterLowerCase()} = ${parameter.Name};");
            }
        }

        private void WriteMethodPhpDocs(CodeMethod codeMethod, LanguageWriter writer, IReadOnlyList<string> orNullReturn)
        {
            var methodDescription = codeMethod.Description ?? string.Empty;

            var hasMethodDescription = !string.IsNullOrEmpty(methodDescription.Trim(' '));
            var parametersWithDescription = codeMethod.Parameters;
            var withDescription = parametersWithDescription as CodeParameter[] ?? parametersWithDescription.ToArray();
            if (!hasMethodDescription && !withDescription.Any())
            {
                return;
            }

            writer.WriteLine(conventions.DocCommentStart);
            var isVoidable = "void".Equals(conventions.GetTypeString(codeMethod.ReturnType, codeMethod),
                StringComparison.OrdinalIgnoreCase);
            if(hasMethodDescription){
                writer.WriteLine(
                    $"{conventions.DocCommentPrefix}{methodDescription}");
            }

            var accessedProperty = codeMethod.AccessedProperty;
            var isSetterForAdditionalData = (codeMethod.IsOfKind(CodeMethodKind.Setter) &&
                                             accessedProperty.IsOfKind(CodePropertyKind.AdditionalData));
            var isGetterForAdditionalData = (codeMethod.IsOfKind(CodeMethodKind.Getter) &&
                                             accessedProperty.IsOfKind(CodePropertyKind.AdditionalData));
            
            
            withDescription.Select(x =>
                {
                    return codeMethod.MethodKind switch
                    {
                        CodeMethodKind.Setter => $"{conventions.DocCommentPrefix} @param {(isSetterForAdditionalData ? "array<string,object> $value": conventions.GetParameterDocNullable(x, x))} {x?.Description}",
                        _ => $"{conventions.DocCommentPrefix}@param {conventions.GetParameterDocNullable(x, x)} ${x.Name} {x.Description}"
                    };
                })
                .ToList()
                .ForEach(x => writer.WriteLine(x));
            var returnDocString = codeMethod.MethodKind switch
                {
                    CodeMethodKind.Deserializer => "array<string, callable>",
                    CodeMethodKind.Getter => isGetterForAdditionalData
                        ? "array<string, object>"
                        : conventions.GetTypeString(codeMethod.ReturnType, codeMethod),
                    _ => conventions.GetTypeString(codeMethod.ReturnType, codeMethod)
                };
            if (!isVoidable)
            {
                writer.WriteLines(
                    $"{conventions.DocCommentPrefix}@return {returnDocString}{orNullReturn[1]}"
                    );
            }
            writer.WriteLine(conventions.DocCommentEnd);
        }
        
        /**
         * Writes the method signatures and puts the parameters.
         * for example this writes
         * function methodName(int $parameter, string $parameter2){
         */
        private void WriteMethodsAndParameters(CodeMethod codeMethod, LanguageWriter writer, IReadOnlyList<string> orNullReturn, bool isConstructor = false)
        {
            var methodParameters = string.Join(", ", codeMethod.Parameters.Select(x => conventions.GetParameterSignature(x, codeMethod)).ToList());

            var methodName = codeMethod.MethodKind switch
            {
                (CodeMethodKind.Constructor or CodeMethodKind.ClientConstructor) => "__construct",
                (CodeMethodKind.Getter or CodeMethodKind.Setter) => codeMethod.AccessedProperty?.Name?.ToFirstCharacterUpperCase(),
                _ => codeMethod.Name.ToFirstCharacterLowerCase()
            };
            var methodPrefix = codeMethod.MethodKind switch
            {
                CodeMethodKind.Getter => "get",
                CodeMethodKind.Setter => "set",
                _ => string.Empty
            };
            if(codeMethod.IsOfKind(CodeMethodKind.Deserializer))
            {
                writer.WriteLine($"{conventions.GetAccessModifier(codeMethod.Access)} function getFieldDeserializers(): array {{");
                writer.IncreaseIndent();
                return;
            }

            if (codeMethod.IsOfKind(CodeMethodKind.Getter) && codeMethod.AccessedProperty.IsOfKind(CodePropertyKind.AdditionalData))
            {
                writer.WriteLine($"{conventions.GetAccessModifier(codeMethod.Access)} function {methodPrefix}{methodName}(): array {{");
                writer.IncreaseIndent();
                return;
            }
            var isVoidable = "void".Equals(conventions.GetTypeString(codeMethod.ReturnType, codeMethod),
                StringComparison.OrdinalIgnoreCase);
            var optionalCharacterReturn = isVoidable ? string.Empty : orNullReturn[0];
            var returnValue = isConstructor
                ? string.Empty
                : $": {optionalCharacterReturn}{conventions.GetTypeString(codeMethod.ReturnType, codeMethod)}";
            writer.WriteLine($"{conventions.GetAccessModifier(codeMethod.Access)} function {methodPrefix}{methodName}({methodParameters}){returnValue} {{");
            writer.IncreaseIndent();
            
        }

        private void WriteSerializerBody(CodeClass parentClass, LanguageWriter writer, bool inherits)
        {
            var additionalDataProperty = parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData).FirstOrDefault();
            if(inherits)
                writer.WriteLine("parent::serialize($writer);");
            var customProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom);
            foreach(var otherProp in customProperties) {
                writer.WriteLine($"$writer->{GetSerializationMethodName(otherProp.Type)}('{otherProp.SerializationName ?? otherProp.Name.ToFirstCharacterLowerCase()}', $this->{otherProp.Name.ToFirstCharacterLowerCase()});");
            }
            if(additionalDataProperty != null)
                writer.WriteLine($"$writer->writeAdditionalData($this->{additionalDataProperty.Name.ToFirstCharacterLowerCase()});");
        }
        
        private string GetSerializationMethodName(CodeTypeBase propType) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = conventions.TranslateType(propType);
            if(propType is CodeType currentType) {
                if(isCollection) 
                    return $"writeCollectionOfObjectValues";
                else if(currentType.TypeDefinition is CodeEnum currentEnum)
                    return "writeEnumValue";
            }
            switch(propertyType) {
                case "string" or "Guid":
                    return "writeStringValue";
                case "bool":
                    return "writeBooleanValue";
                case "boolean" or "number" or "Date":
                    return $"write{propertyType.ToFirstCharacterUpperCase()}Value";
                default:
                    return $"writeObjectValue";
            }
        }

        private void WriteSetterBody(LanguageWriter writer, CodeMethod codeElement)
        {
            var propertyName = codeElement.AccessedProperty?.Name;
            writer.WriteLine($"$this->{propertyName.ToFirstCharacterLowerCase()} = $value;");
        }

        private void WriteGetterBody(LanguageWriter writer, CodeMethod codeMethod)
        {
            var propertyName = codeMethod.AccessedProperty?.Name.ToFirstCharacterLowerCase();
            writer.WriteLine($"return $this->{propertyName};");
        }

        private static void WriteDeserializerBody(LanguageWriter writer, CodeMethod codeMethod)
        {
            writer.WriteLine("echo('This is the body of the deserializer.');");
        }
    }
}
