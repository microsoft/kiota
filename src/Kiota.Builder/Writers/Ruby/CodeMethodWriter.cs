﻿using System;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Ruby {
    public class CodeMethodWriter : BaseElementWriter<CodeMethod, RubyConventionService>
    {
        public CodeMethodWriter(RubyConventionService conventionService) : base(conventionService){
        }
        public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
        {
            ArgumentNullException.ThrowIfNull(codeElement);
            ArgumentNullException.ThrowIfNull(writer);
            if(!(codeElement.Parent is CodeClass)) throw new InvalidOperationException("the parent of a method should be a class");
            var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement);
            WriteMethodDocumentation(codeElement, writer);
            var parentClass = codeElement.Parent as CodeClass;
            var inherits = parentClass.StartBlock.Inherits != null;
            var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
            var config = codeElement.Parameters.OfKind(CodeParameterKind.RequestConfiguration);
            var requestParams = new RequestParams(requestBodyParam, config);
            switch(codeElement.Kind) {
                case CodeMethodKind.Serializer:
                    WriteMethodPrototype(codeElement, writer);
                    WriteSerializerBody(parentClass, writer);
                break;
                case CodeMethodKind.Deserializer:
                    WriteMethodPrototype(codeElement, writer);
                    WriteDeserializerBody(parentClass, writer);
                break;
                case CodeMethodKind.IndexerBackwardCompatibility:
                    WriteMethodPrototype(codeElement, writer);
                    WriteIndexerBody(codeElement, parentClass, writer, returnType);
                break;
                case CodeMethodKind.RequestGenerator:
                    WriteMethodPrototype(codeElement, writer);
                    WriteRequestGeneratorBody(codeElement, requestParams, parentClass, writer);
                break;
                case CodeMethodKind.RequestExecutor:
                    WriteMethodPrototype(codeElement, writer);
                    WriteRequestExecutorBody(codeElement, requestParams, returnType, writer);
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
                case CodeMethodKind.RequestBuilderWithParameters:
                    WriteRequestBuilderBody(parentClass, codeElement, writer);
                    break;
                case CodeMethodKind.RequestBuilderBackwardCompatibility:
                    throw new InvalidOperationException("RequestBuilderBackwardCompatibility is not supported as the request builders are implemented by properties.");
                default:
                    WriteMethodPrototype(codeElement, writer);
                    writer.WriteLine("return nil;");
                break;
            }
            writer.CloseBlock("end");
        }
        private void WriteRequestBuilderBody(CodeClass parentClass, CodeMethod codeElement, LanguageWriter writer)
        {
            var importSymbol = conventions.GetTypeString(codeElement.ReturnType, parentClass);
            conventions.AddRequestBuilderBody(parentClass, importSymbol, writer, prefix: "return ", pathParameters: codeElement.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Path)));
        }
        private static void WriteApiConstructorBody(CodeClass parentClass, CodeMethod method, LanguageWriter writer) {
            var requestAdapterProperty = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
            var requestAdapterPropertyName = requestAdapterProperty.Name.ToSnakeCase();
            if (method.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.RequestAdapter)) is CodeParameter requestAdapterParameter)
                writer.WriteLine($"@{requestAdapterPropertyName} = {requestAdapterParameter.Name.ToSnakeCase()}");
            if(!string.IsNullOrEmpty(method.BaseUrl))
                writer.WriteLine($"{requestAdapterPropertyName}.set_base_url('{method.BaseUrl}')");
        }
        private static void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer, bool inherits) {
            if(inherits)
                writer.WriteLine("super");
            foreach(var propWithDefault in parentClass.GetPropertiesOfKind(CodePropertyKind.BackingStore,
                                                                            CodePropertyKind.RequestBuilder,
                                                                            CodePropertyKind.UrlTemplate,
                                                                            CodePropertyKind.PathParameters)
                                            .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
                                            .OrderBy(x => x.Name)) {
                writer.WriteLine($"@{propWithDefault.NamePrefix}{propWithDefault.Name.ToSnakeCase()} = {propWithDefault.DefaultValue}");
            }
            if(currentMethod.IsOfKind(CodeMethodKind.Constructor)) {
                AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.RequestAdapter, CodePropertyKind.RequestAdapter, writer);
                if(parentClass.IsOfKind(CodeClassKind.RequestBuilder)) {
                    var pathParametersParam = currentMethod.Parameters.OfKind(CodeParameterKind.PathParameters);
                    var pathParametersParamName = pathParametersParam.Name.ToSnakeCase();
                    writer.WriteLine($"if {pathParametersParamName}.is_a? String");
                    writer.IncreaseIndent();
                    writer.WriteLine($"{pathParametersParamName} = {{ \"request-raw-url\" => {pathParametersParamName} }}");
                    writer.CloseBlock("end");
                }
                AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.PathParameters, CodePropertyKind.PathParameters, writer);
            }
        }
        private static void AssignPropertyFromParameter(CodeClass parentClass, CodeMethod currentMethod, CodeParameterKind parameterKind, CodePropertyKind propertyKind, LanguageWriter writer) {
            var property = parentClass.GetPropertyOfKind(propertyKind);
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
        private void WriteIndexerBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer, string returnType) {
            var pathParametersProperty = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
            var prefix = conventions.GetNormalizedNamespacePrefixForType(codeElement.ReturnType);
            writer.WriteLines($"{conventions.TempDictionaryVarName} = @{pathParametersProperty.Name.ToSnakeCase()}.clone",
                            $"{conventions.TempDictionaryVarName}[\"{codeElement.OriginalIndexer.SerializationName}\"] = id");
            conventions.AddRequestBuilderBody(parentClass, returnType, writer, conventions.TempDictionaryVarName, $"return {prefix}");
        }
        private void WriteDeserializerBody(CodeClass parentClass, LanguageWriter writer) {
            if(parentClass.StartBlock.Inherits != null)
                writer.WriteLine("return super.merge({");
            else
                writer.WriteLine("return {");
            writer.IncreaseIndent();
            foreach(var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom).Where(static x => !x.ExistsInBaseType)) {
                writer.WriteLine($"\"{otherProp.SerializationName ?? otherProp.Name.ToFirstCharacterLowerCase()}\" => lambda {{|n| @{otherProp.Name.ToSnakeCase()} = n.{GetDeserializationMethodName(otherProp.Type)} }},");
            }
            writer.DecreaseIndent();
            if(parentClass.StartBlock.Inherits != null)
                writer.WriteLine("})");
            else
                writer.WriteLine("}");
        }
        private void WriteRequestExecutorBody(CodeMethod codeElement, RequestParams requestParams, string returnType, LanguageWriter writer) {
            if(returnType.Equals("void", StringComparison.OrdinalIgnoreCase))
            {
                if(codeElement.IsOfKind(CodeMethodKind.RequestExecutor))
                    returnType = "nil"; //generic type for the future
            } else if (codeElement.ReturnType is CodeType returnT && returnT.TypeDefinition is not null) {
                returnType = $"{returnT.TypeDefinition.GetImmediateParentOfType<CodeNamespace>().Name.NormalizeNameSpaceName("::")}::{returnType}";
            }
            if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
            

            var generatorMethodName = (codeElement.Parent as CodeClass)
                                                .Methods
                                                .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod)
                                                ?.Name
                                                ?.ToFirstCharacterLowerCase();
            writer.WriteLine($"request_info = self.{generatorMethodName.ToSnakeCase()}(");
            var requestInfoParameters = new[] { requestParams.requestBody, requestParams.requestConfiguration }
                .Where(x => x != null)
                .Select(x => x.Name.ToSnakeCase());
            if(requestInfoParameters.Any()) {
                writer.IncreaseIndent();
                writer.WriteLine(requestInfoParameters.Aggregate((x,y) => $"{x}, {y}"));
                writer.DecreaseIndent();
            }
            writer.WriteLine(")");
            var isStream = conventions.StreamTypeName.Equals(StringComparison.OrdinalIgnoreCase);
            var genericTypeForSendMethod = GetSendRequestMethodName(isStream);
            writer.WriteLine($"return @request_adapter.{genericTypeForSendMethod}(request_info, {returnType}, response_handler)");
        }

        private void WriteRequestGeneratorBody(CodeMethod codeElement, RequestParams requestParams, CodeClass parentClass, LanguageWriter writer) {
            if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");

            var urlTemplateParamsProperty = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
            var urlTemplateProperty = parentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate);
            var requestAdapterProperty = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
            writer.WriteLines("request_info = MicrosoftKiotaAbstractions::RequestInformation.new()",
                                $"request_info.url_template = {GetPropertyCall(urlTemplateProperty, "''")}",
                                $"request_info.path_parameters = {GetPropertyCall(urlTemplateParamsProperty, "''")}",
                                $"request_info.http_method = :{codeElement.HttpMethod?.ToString().ToUpperInvariant()}");
            if(codeElement.AcceptedResponseTypes.Any())
                writer.WriteLine($"request_info.headers['Accept'] = '{string.Join(", ", codeElement.AcceptedResponseTypes)}'");
            if (requestParams.requestConfiguration != null)
            {
                var queryString = requestParams.QueryParameters;
                var headers = requestParams.Headers;
                // TODO add options handling
                if(headers != null || queryString != null) {
                    writer.WriteLine($"unless {requestParams.requestConfiguration.Name.ToSnakeCase()}.nil?"); 
                    writer.IncreaseIndent();
                    if(headers != null)
                        writer.WriteLine($"request_info.set_headers_from_raw_object({requestParams.requestConfiguration.Name.ToSnakeCase()}.{headers.Name.ToSnakeCase()})");
                    if(queryString != null)
                        writer.WriteLines($"request_info.set_query_string_parameters_from_raw_object({requestParams.requestConfiguration.Name.ToSnakeCase()}.{queryString.Name.ToSnakeCase()})");
                    writer.CloseBlock("end");
                }
                if(requestParams.requestBody != null) {
                    if(requestParams.requestBody.Type.Name.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
                        writer.WriteLine($"request_info.set_stream_content({requestParams.requestBody.Name})");
                    else
                        writer.WriteLine($"request_info.set_content_from_parsable(self.{requestAdapterProperty.Name.ToSnakeCase()}, \"{codeElement.RequestBodyContentType}\", {requestParams.requestBody.Name})");
                }
            }
            writer.WriteLine("return request_info");
        }
        private static string GetPropertyCall(CodeProperty property, string defaultValue) => property == null ? defaultValue : $"@{property.Name.ToSnakeCase()}";
        private void WriteSerializerBody(CodeClass parentClass, LanguageWriter writer) {
            var additionalDataProperty = parentClass.GetPropertyOfKind(CodePropertyKind.AdditionalData);
            if(parentClass.StartBlock.Inherits != null)
                writer.WriteLine("super");
            foreach(var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom).Where(static x => !x.ExistsInBaseType && !x.ReadOnly)) {
                writer.WriteLine($"writer.{GetSerializationMethodName(otherProp.Type)}(\"{otherProp.SerializationName ?? otherProp.Name.ToFirstCharacterLowerCase()}\", @{otherProp.Name.ToSnakeCase()})");
            }
            if(additionalDataProperty != null)
                writer.WriteLine($"writer.write_additional_data(@{additionalDataProperty.Name.ToSnakeCase()})");
        }
        private static readonly BaseCodeParameterOrderComparer parameterOrderComparer = new();
        private void WriteMethodPrototype(CodeMethod code, LanguageWriter writer) {
            var methodName = code.Kind switch {
                CodeMethodKind.Constructor or CodeMethodKind.ClientConstructor => "initialize",
                CodeMethodKind.Getter => $"{code.AccessedProperty?.Name?.ToSnakeCase()}",
                CodeMethodKind.Setter => $"{code.AccessedProperty?.Name?.ToSnakeCase()}",
                _ => code.Name.ToSnakeCase()
            };
            var parameters = string.Join(", ", code.Parameters.OrderBy(x => x, parameterOrderComparer).Select(p=> conventions.GetParameterSignature(p, code).ToSnakeCase()).ToList());
            writer.WriteLine($"def {methodName.ToSnakeCase()}({parameters}) ");
            writer.IncreaseIndent();
        }
        private void WriteMethodDocumentation(CodeMethod code, LanguageWriter writer) {
            var isDescriptionPresent = !string.IsNullOrEmpty(code.Documentation.Description);
            var parametersWithDescription = code.Parameters.Where(x => !string.IsNullOrEmpty(code.Documentation.Description));
            if (isDescriptionPresent || parametersWithDescription.Any()) {
                writer.WriteLine(conventions.DocCommentStart);
                if(isDescriptionPresent)
                    writer.WriteLine($"{conventions.DocCommentPrefix}{RubyConventionService.RemoveInvalidDescriptionCharacters(code.Documentation.Description)}");
                foreach(var paramWithDescription in parametersWithDescription.OrderBy(x => x.Name))
                    writer.WriteLine($"{conventions.DocCommentPrefix}@param {paramWithDescription.Name} {RubyConventionService.RemoveInvalidDescriptionCharacters(paramWithDescription.Documentation.Description)}");
                
                if(code.IsAsync)
                    writer.WriteLine($"{conventions.DocCommentPrefix}@return a CompletableFuture of {code.ReturnType.Name.ToSnakeCase()}");
                else
                    writer.WriteLine($"{conventions.DocCommentPrefix}@return a {code.ReturnType.Name.ToSnakeCase()}");
                writer.WriteLine(conventions.DocCommentEnd);
            }
        }
        private string GetDeserializationMethodName(CodeTypeBase propType) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = conventions.TranslateType(propType);
            if(propType is CodeType currentType)
            {
                if(isCollection)
                    if(currentType.TypeDefinition == null)
                        return $"get_collection_of_primitive_values({TranslateObjectType(propertyType.ToFirstCharacterUpperCase())})";
                    else
                        return $"get_collection_of_object_values({(propType as CodeType).TypeDefinition.Parent.Name.NormalizeNameSpaceName("::").ToFirstCharacterUpperCase()}::{propertyType.ToFirstCharacterUpperCase()})";
                if(currentType.TypeDefinition is CodeEnum currentEnum)
                    return $"get_enum_value{(currentEnum.Flags ? "s" : string.Empty)}({(propType as CodeType).TypeDefinition.Parent.Name.NormalizeNameSpaceName("::").ToFirstCharacterUpperCase()}::{propertyType.ToFirstCharacterUpperCase()})";
            }
            return propertyType switch
            {
                "string" or "boolean" or "number" or "float" or "Guid" => $"get_{propertyType.ToSnakeCase()}_value()",
                "DateTimeOffset" or "DateTime" => "get_date_time_value()",
                "TimeSpan" or "MicrosoftKiotaAbstractions::ISODuration" => "get_duration_value()",
                "DateOnly" or "Date" => "get_date_value()",
                "TimeOnly" or "Time" => "get_time_value()",
                _ => $"get_object_value({(propType as CodeType).TypeDefinition?.Parent?.Name.NormalizeNameSpaceName("::").ToFirstCharacterUpperCase()}::{propertyType.ToFirstCharacterUpperCase()})",
            };
        }
        private static string TranslateObjectType(string typeName)
        {
            return typeName switch {
                "String" or "Float" or "Object" => typeName, 
                "Boolean" => "\"boolean\"",
                "Number" => "Integer",
                "Guid" => "UUIDTools::UUID",
                "Date" => "Time",
                "DateTimeOffset" => "Time",
                _ => typeName.ToFirstCharacterUpperCase() ?? "Object",
            };
        }
        private string GetSerializationMethodName(CodeTypeBase propType) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = conventions.TranslateType(propType);
            if(propType is CodeType currentType)
            {
                if (isCollection)
                    if(currentType.TypeDefinition == null)
                        return "write_collection_of_primitive_values";
                    else
                        return "write_collection_of_object_values";
                if(currentType.TypeDefinition is CodeEnum)
                    return "write_enum_value";
            }
            return propertyType switch
            {
                "string" or "boolean" or "number" or "float" or "Guid" => $"write_{propertyType.ToSnakeCase()}_value",
                "DateTimeOffset" or "DateTime" => "write_date_time_value",
                "TimeSpan" or "MicrosoftKiotaAbstractions::ISODuration" => "write_duration_value",
                "DateOnly" or "Date" => "write_date_value",
                "TimeOnly" or "Time" => "write_time_value",
                _ => "write_object_value",
            };
        }
        private static string GetSendRequestMethodName(bool isStream)
        {
            if(isStream) return "send_primitive_async";
            return "send_async";
        }
    }
}
