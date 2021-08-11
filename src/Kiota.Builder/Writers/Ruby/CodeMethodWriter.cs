using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Extensions;

namespace Kiota.Builder.Writers.Ruby {
    public class CodeMethodWriter : BaseElementWriter<CodeMethod, RubyConventionService>
    {
        public CodeMethodWriter(RubyConventionService conventionService) : base(conventionService){
        }
        public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
        {
            if(codeElement == null) throw new ArgumentNullException(nameof(codeElement));
            if(writer == null) throw new ArgumentNullException(nameof(writer));
            if(!(codeElement.Parent is CodeClass)) throw new InvalidOperationException("the parent of a method should be a class");
            WriteMethodDocumentation(codeElement, writer);
            var parentClass = codeElement.Parent as CodeClass;
            var inherits = (parentClass.StartBlock as CodeClass.Declaration).Inherits != null;
            var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
            var queryStringParam = codeElement.Parameters.OfKind(CodeParameterKind.QueryParameter);
            var headersParam = codeElement.Parameters.OfKind(CodeParameterKind.Headers);
            switch(codeElement.MethodKind) {
                case CodeMethodKind.Serializer:
                    WriteMethodPrototype(codeElement, writer);
                    WriteSerializerBody(parentClass, writer);
                break;
                case CodeMethodKind.Deserializer:
                    WriteMethodPrototype(codeElement, writer);
                    WriteDeserializerBody(parentClass, writer);
                break;
                case CodeMethodKind.RequestGenerator:
                    WriteMethodPrototype(codeElement, writer);
                    WriteRequestGeneratorBody(codeElement, requestBodyParam, queryStringParam, headersParam, writer);
                break;
                case CodeMethodKind.RequestExecutor:
                    WriteMethodPrototype(codeElement, writer);
                    WriteRequestExecutorBody(codeElement, requestBodyParam, queryStringParam, headersParam, writer);
                break;
                case CodeMethodKind.Getter:
                    WriteGetterBody(codeElement, writer);
                    break;
                case CodeMethodKind.Setter:
                    WriteSetterBody(codeElement, writer);
                    break;
                case CodeMethodKind.ClientConstructor:
                    WriteMethodPrototype(codeElement, writer);
                    WriteConstructorBody(parentClass, codeElement, writer, inherits);
                    WriteApiConstructorBody(parentClass, codeElement, writer);
                break;
                case CodeMethodKind.Constructor:
                    WriteMethodPrototype(codeElement, writer);
                    WriteConstructorBody(parentClass, codeElement, writer, inherits);
                    break;
                default:
                    WriteMethodPrototype(codeElement, writer);
                    writer.WriteLine("return nil;");
                break;
            }
            writer.DecreaseIndent();
            writer.WriteLine("end");
        }
        private static void WriteApiConstructorBody(CodeClass parentClass, CodeMethod method, LanguageWriter writer) {
            var httpCoreProperty = parentClass.GetChildElements(true).OfType<CodeProperty>().FirstOrDefault(x => x.IsOfKind(CodePropertyKind.HttpCore));
            var httpCoreParameter = method.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.HttpCore));
            var httpCorePropertyName = httpCoreProperty.Name.ToSnakeCase();
            writer.WriteLine($"@{httpCorePropertyName} = {httpCoreParameter.Name}");
        }
        private static void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer, bool inherits) {
            if(inherits)
                writer.WriteLine("super");
            foreach(var propWithDefault in parentClass.GetPropertiesOfKind(CodePropertyKind.BackingStore,
                                                                            CodePropertyKind.RequestBuilder,
                                                                            CodePropertyKind.PathSegment)
                                            .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
                                            .OrderBy(x => x.Name)) {
                writer.WriteLine($"@{propWithDefault.NamePrefix}{propWithDefault.Name.ToSnakeCase()} = {propWithDefault.DefaultValue}");
            }
            if(currentMethod.IsOfKind(CodeMethodKind.Constructor)) {
                AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.HttpCore, CodePropertyKind.HttpCore, writer);
                AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.CurrentPath, CodePropertyKind.CurrentPath, writer);
            }
        }
        private static void AssignPropertyFromParameter(CodeClass parentClass, CodeMethod currentMethod, CodeParameterKind parameterKind, CodePropertyKind propertyKind, LanguageWriter writer) {
            var property = parentClass.GetChildElements(true).OfType<CodeProperty>().FirstOrDefault(x => x.IsOfKind(propertyKind));
            var parameter = currentMethod.Parameters.FirstOrDefault(x => x.IsOfKind(parameterKind));
            if(property != null && parameter != null) {
                writer.WriteLine($"@{property.Name.ToSnakeCase()} = {parameter.Name.ToSnakeCase()}");
            }
        }
        private static void WriteSetterBody(CodeMethod codeElement, LanguageWriter writer) {
            writer.WriteLine($"def  {codeElement.AccessedProperty?.Name?.ToSnakeCase()}=({codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()})");
            writer.IncreaseIndent();
            writer.WriteLine($"@{codeElement.AccessedProperty?.Name?.ToSnakeCase()} = {codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()}");
        }
        private static void WriteGetterBody(CodeMethod codeElement, LanguageWriter writer) {
            writer.WriteLine($"def  {codeElement.AccessedProperty?.Name?.ToSnakeCase()}");
            writer.IncreaseIndent();
            writer.WriteLine($"return @{codeElement.AccessedProperty?.Name?.ToSnakeCase()}");
        }
        private void WriteDeserializerBody(CodeClass parentClass, LanguageWriter writer) {
            if((parentClass.StartBlock as CodeClass.Declaration).Inherits != null)
                writer.WriteLine("return super.merge({");
            else
                writer.WriteLine($"return {{");
            writer.IncreaseIndent();
            foreach(var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)) {
                writer.WriteLine($"\"{otherProp.SerializationName ?? otherProp.Name.ToSnakeCase()}\" => lambda {{|o, n| o.{otherProp.Name.ToSnakeCase()} = n.{GetDeserializationMethodName(otherProp.Type)} }},");
            }
            writer.DecreaseIndent();
            if((parentClass.StartBlock as CodeClass.Declaration).Inherits != null)
                writer.WriteLine("})");
            else
                writer.WriteLine("}");
        }
        private void WriteRequestExecutorBody(CodeMethod codeElement, CodeParameter requestBodyParam, CodeParameter queryStringParam, CodeParameter headersParam , LanguageWriter writer) {
            if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
            

            var generatorMethodName = (codeElement.Parent as CodeClass)
                                                .GetChildElements(true)
                                                .OfType<CodeMethod>()
                                                .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod)
                                                ?.Name
                                                ?.ToFirstCharacterLowerCase();
            writer.WriteLine($"request_info = self.{generatorMethodName.ToSnakeCase()}(");
            var requestInfoParameters = new List<string> { requestBodyParam?.Name, queryStringParam?.Name, headersParam?.Name }.Where(x => x != null);
            if(requestInfoParameters.Any()) {
                writer.IncreaseIndent();
                writer.WriteLine(requestInfoParameters.Aggregate((x,y) => $"{x.ToSnakeCase()}, {y.ToSnakeCase()}"));
                writer.DecreaseIndent();
            }
            writer.WriteLine(")");
            var isStream = conventions.StreamTypeName.Equals(StringComparison.OrdinalIgnoreCase);
            var genericTypeForSendMethod = GetSendRequestMethodName(isStream);
            writer.WriteLine($"return self.http_core.{genericTypeForSendMethod}(request_info, response_handler)");
        }

        private void WriteRequestGeneratorBody(CodeMethod codeElement, CodeParameter requestBodyParam, CodeParameter queryStringParam, CodeParameter headersParam, LanguageWriter writer) {
            if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
            writer.WriteLines("request_info = RequestInfo.new()",
                                $"request_info.URI = {conventions.CurrentPathPropertyName} + {conventions.PathSegmentPropertyName}",
                                $"request_info.http_method = :{codeElement.HttpMethod?.ToString().ToUpperInvariant()}");
            if(headersParam != null)
                writer.WriteLine($"request_info.set_headers_from_raw_object(h)");
            if(queryStringParam != null)
                writer.WriteLines($"request_info.set_query_string_parameters_from_raw_object(q)");
            if(requestBodyParam != null) {
                if(requestBodyParam.Type.Name.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
                    writer.WriteLine($"request_info.set_stream_content({requestBodyParam.Name})");
                else
                    writer.WriteLine($"request_info.set_content_from_parsable({requestBodyParam.Name}, self.{RubyConventionService.SerializerFactoryPropertyName}, \"{codeElement.ContentType}\")");
            }
            writer.WriteLine("return request_info;");
        }
        private void WriteSerializerBody(CodeClass parentClass, LanguageWriter writer) {
            var additionalDataProperty = parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData).FirstOrDefault();
            if((parentClass.StartBlock as CodeClass.Declaration).Inherits != null)
                writer.WriteLine("super");
            foreach(var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)) {
                writer.WriteLine($"writer.{GetSerializationMethodName(otherProp.Type)}(\"{otherProp.SerializationName ?? otherProp.Name.ToSnakeCase()}\", self.{otherProp.Name.ToSnakeCase()})");
            }
            if(additionalDataProperty != null)
                writer.WriteLine($"writer.write_additional_data(@{additionalDataProperty.Name.ToSnakeCase()})");
        }
        private void WriteMethodPrototype(CodeMethod code, LanguageWriter writer) {
            var methodName = (code.MethodKind switch {
                (CodeMethodKind.Constructor or CodeMethodKind.ClientConstructor) => $"initialize",
                (CodeMethodKind.Getter) => $"{code.AccessedProperty?.Name?.ToSnakeCase()}",
                (CodeMethodKind.Setter) => $"{code.AccessedProperty?.Name?.ToSnakeCase()}",
                _ => code.Name.ToSnakeCase()
            });
            var parameters = string.Join(", ", code.Parameters.Select(p=> conventions.GetParameterSignature(p).ToSnakeCase()).ToList());
            writer.WriteLine($"def {methodName.ToSnakeCase()}({parameters}) ");
            writer.IncreaseIndent();
        }
        private void WriteMethodDocumentation(CodeMethod code, LanguageWriter writer) {
            var isDescriptionPresent = !string.IsNullOrEmpty(code.Description);
            var parametersWithDescription = code.Parameters.Where(x => !string.IsNullOrEmpty(code.Description));
            if (isDescriptionPresent || parametersWithDescription.Any()) {
                writer.WriteLine(conventions.DocCommentStart);
                if(isDescriptionPresent)
                    writer.WriteLine($"{conventions.DocCommentPrefix}{RubyConventionService.RemoveInvalidDescriptionCharacters(code.Description)}");
                foreach(var paramWithDescription in parametersWithDescription.OrderBy(x => x.Name))
                    writer.WriteLine($"{conventions.DocCommentPrefix}@param {paramWithDescription.Name} {RubyConventionService.RemoveInvalidDescriptionCharacters(paramWithDescription.Description)}");
                
                if(code.IsAsync)
                    writer.WriteLine($"{conventions.DocCommentPrefix}@return a CompletableFuture of {code.ReturnType.Name.ToSnakeCase()}");
                else
                    writer.WriteLine($"{conventions.DocCommentPrefix}@return a {code.ReturnType.Name.ToSnakeCase()}");
                writer.WriteLine(conventions.DocCommentEnd);
            }
        }
        private string GetDeserializationMethodName(CodeTypeBase propType) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = conventions.TranslateType(propType.Name);
            if(propType is CodeType currentType) {
                if(isCollection)
                    if(currentType.TypeDefinition == null)
                        return $"get_collection_of_primitive_values({TranslateObjectType(propertyType).ToFirstCharacterUpperCase()})";
                    else
                        return $"get_collection_of_object_values({(propType as CodeType).TypeDefinition.Parent.Name.NormalizeNameSpaceName("::").ToFirstCharacterUpperCase()}::{propertyType.ToFirstCharacterUpperCase()})";
                else if(currentType.TypeDefinition is CodeEnum currentEnum)
                    return $"get_enum_value{(currentEnum.Flags ? "s" : string.Empty)}({(propType as CodeType).TypeDefinition.Parent.Name.NormalizeNameSpaceName("::").ToFirstCharacterUpperCase()}::{propertyType.ToFirstCharacterUpperCase()})";
            }
            switch(propertyType) {
                case "string":
                case "boolean":
                case "number":
                case "float":
                case "Guid":
                    return $"get_{propertyType.ToSnakeCase()}_value()";
                case "DateTimeOffset":
                case "Date":
                    return $"get_date_value()";
                default:
                    return $"get_object_value({(propType as CodeType).TypeDefinition.Parent.Name.NormalizeNameSpaceName("::").ToFirstCharacterUpperCase()}::{propertyType.ToFirstCharacterUpperCase()})";
            }
        }
        private static string TranslateObjectType(string typeName)
        {
            return (typeName) switch {
                "string" or "float" or "object" => typeName, 
                "boolean" => "\"boolean\"",
                "number" => "Integer",
                "Guid" => "UUIDTools::UUID",
                "Date" => "Time",
                "DateTimeOffset" => "Time",
                _ => typeName.ToFirstCharacterUpperCase() ?? "object",
            };
        }
        private string GetSerializationMethodName(CodeTypeBase propType) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = conventions.TranslateType(propType.Name);
            if(propType is CodeType currentType) {
                if(isCollection)
                    if(currentType.TypeDefinition == null)
                        return $"write_collection_of_primitive_values";
                    else
                        return $"write_collection_of_object_values";
                else if(currentType.TypeDefinition is CodeEnum currentEnum)
                    return $"write_enum_value";
            }
            switch(propertyType) {
                case "string":
                case "boolean":
                case "number":
                case "float":
                case "Guid":
                    return $"write_{propertyType.ToSnakeCase()}_value";
                case "DateTimeOffset":
                case "Date":
                    return $"write_date_value";
                default:
                    return $"write_object_value";
            }
        }
        private static string GetSendRequestMethodName(bool isStream) {
            if(isStream) return $"send_primitive_async";
            else return $"send_async";
        }
    }
}
