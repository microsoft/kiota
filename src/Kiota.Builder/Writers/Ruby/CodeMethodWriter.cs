using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Extensions;

namespace Kiota.Builder.Writers.Ruby {
    public class CodeMethodWriter : BaseElementWriter<CodeMethod, RubyConventionService>
    {
        public CodeMethodWriter(RubyConventionService conventionService) : base(conventionService){
            //TODO
        }
        public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
        {
            //TODO
            if(codeElement == null) throw new ArgumentNullException(nameof(codeElement));
            if(writer == null) throw new ArgumentNullException(nameof(writer));
            if(!(codeElement.Parent is CodeClass)) throw new InvalidOperationException("the parent of a method should be a class");
            WriteMethodDocumentation(codeElement, writer);
            WriteMethodPrototype(codeElement, writer);
            writer.WriteLine();
            writer.IncreaseIndent();
            var parentClass = codeElement.Parent as CodeClass;
            var inherits = (parentClass.StartBlock as CodeClass.Declaration).Inherits != null;
            var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
            var queryStringParam = codeElement.Parameters.OfKind(CodeParameterKind.QueryParameter);
            var headersParam = codeElement.Parameters.OfKind(CodeParameterKind.Headers);
            switch(codeElement.MethodKind) {
                case CodeMethodKind.Serializer:
                    WriteSerializerBody(parentClass, writer);
                break;
                case CodeMethodKind.Deserializer:
                    WriteDeserializerBody(codeElement, parentClass, writer);
                break;
                case CodeMethodKind.RequestGenerator:
                    WriteRequestGeneratorBody(codeElement, requestBodyParam, queryStringParam, headersParam, writer);
                break;
                case CodeMethodKind.RequestExecutor:
                    WriteRequestExecutorBody(codeElement, requestBodyParam, queryStringParam, headersParam, writer);
                break;
                case CodeMethodKind.Getter:
                    WriteGetterBody(codeElement, writer, parentClass);
                    break;
                case CodeMethodKind.Setter:
                    WriteSetterBody(codeElement, writer, parentClass);
                    break;
                case CodeMethodKind.Constructor:
                    WriteConstructorBody(parentClass, writer, inherits);
                    break;
                default:
                    writer.WriteLine("return nil;");
                break;
            }
            writer.DecreaseIndent();
            writer.WriteLine("end");
        }
        private static void WriteConstructorBody(CodeClass parentClass, LanguageWriter writer, bool inherits) {
            if(inherits)
                writer.WriteLine("super()");
            foreach(var propWithDefault in parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData,
                                                                            CodePropertyKind.BackingStore,
                                                                            CodePropertyKind.RequestBuilder,
                                                                            CodePropertyKind.PathSegment)
                                            .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
                                            .OrderByDescending(x => x.PropertyKind)
                                            .ThenBy(x => x.Name)) {
                writer.WriteLine($"@{propWithDefault.NamePrefix}{propWithDefault.Name.ToSnakeCase()} = {propWithDefault.DefaultValue}");
            }
        }
        private static void WriteSetterBody(CodeMethod codeElement, LanguageWriter writer, CodeClass parentClass) {
            writer.WriteLine($"def  {codeElement.AccessedProperty?.Name?.ToSnakeCase()}({codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()})");
            writer.IncreaseIndent();
            writer.WriteLine($"@{codeElement.AccessedProperty?.Name?.ToSnakeCase()} = ({codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()})");
            writer.DecreaseIndent();
            writer.WriteLine("end");
        }
        private void WriteGetterBody(CodeMethod codeElement, LanguageWriter writer, CodeClass parentClass) {
            writer.WriteLine($"def  {codeElement.AccessedProperty?.Name?.ToSnakeCase()}");
            writer.IncreaseIndent();
            writer.WriteLine($"return @{codeElement.AccessedProperty?.Name?.ToSnakeCase()}");
            writer.DecreaseIndent();
            writer.WriteLine("end");
        }
        private void WriteDeserializerBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer) {
            var inherits = (parentClass.StartBlock as CodeClass.Declaration).Inherits != null;
            writer.WriteLine($"return {{");
            writer.IncreaseIndent();
            var parentClassName = parentClass.Name.ToSnakeCase();
            foreach(var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)) {
                writer.WriteLine($"\"{otherProp.SerializationName ?? otherProp.Name.ToSnakeCase()}\" => {{|o, n| o.{otherProp.Name.ToSnakeCase()} = n.{GetDeserializationMethodName(otherProp.Type)} }},");
            }
            writer.DecreaseIndent();
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

                // var httpMethodPrefix = codeElement.HttpMethod.ToString().ToFirstCharacterUpperCase();
                // writer.WriteLine($"if ({queryStringParam.Name.ToSnakeCase()} != null)");
                // writer.IncreaseIndent();
                // writer.WriteLines($"q_params = {httpMethodPrefix}_query_parameters.new()",
                //             $"{queryStringParam.Name.ToSnakeCase()}.accept(q_params)",
                //             "q_params.add_query_parameters(request_info.query_parameters)");
                // writer.DecreaseIndent();
                // writer.WriteLine("end");
            if(requestBodyParam != null) {
                if(requestBodyParam.Type.Name.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
                    writer.WriteLine($"request_info.set_stream_content({requestBodyParam.Name})");
                else
                    writer.WriteLine($"request_info.set_content_from_parsable({requestBodyParam.Name}, self.{conventions.SerializerFactoryPropertyName}, \"{codeElement.ContentType}\")");
            }
            writer.WriteLine("return request_info;");
        }
        private void WriteSerializerBody(CodeClass parentClass, LanguageWriter writer) {
            var additionalDataProperty = parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData).FirstOrDefault();
            if((parentClass.StartBlock as CodeClass.Declaration).Inherits != null)
                writer.WriteLine("super.serialize(writer)");
            foreach(var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)) {
                writer.WriteLine($"writer.{GetSerializationMethodName(otherProp.Type)}(\"{otherProp.SerializationName ?? otherProp.Name.ToSnakeCase()}\", self.{otherProp.Name.ToSnakeCase()})");
            }
            if(additionalDataProperty != null)
                writer.WriteLine($"writer.write_additional_data(self.{additionalDataProperty.Name.ToSnakeCase()})");
        }
        private void WriteMethodPrototype(CodeMethod code, LanguageWriter writer) {
            var isConstructor = code.IsOfKind(CodeMethodKind.Constructor);
            var methodName = (code.IsOfKind(CodeMethodKind.Getter, CodeMethodKind.Setter) ?
                code.AccessedProperty?.Name :
                code.Name
            ).ToSnakeCase();
            var parameters = string.Join(", ", code.Parameters.Select(p=> conventions.GetParameterSignature(p).ToSnakeCase()).ToList());
            writer.WriteLine($"def {methodName.ToSnakeCase()}({parameters}) {{");
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
                        return $"get_collection_of_primitive_values";
                    else
                        return $"get_collection_of_object_values({propertyType.ToSnakeCase()})";
                else if(currentType.TypeDefinition is CodeEnum currentEnum)
                    return $"get_enum_value{(currentEnum.Flags ? "s" : string.Empty)}({propertyType.ToSnakeCase()})";
            }
            switch(propertyType) {
                case "string":
                case "boolean":
                case "number":
                case "Guid":
                case "Date":
                    return $"get_{propertyType.ToSnakeCase()}_value()";
                default:
                    return $"get_object_value({propertyType.ToSnakeCase()})";
            }
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
                case "Guid":
                case "Date":
                    return $"write_{propertyType.ToSnakeCase()}_value";
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
