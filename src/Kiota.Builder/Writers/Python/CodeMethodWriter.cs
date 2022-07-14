using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Extensions;

namespace Kiota.Builder.Writers.Python {
    public class CodeMethodWriter : BaseElementWriter<CodeMethod, PythonConventionService>
    {
        public CodeMethodWriter(PythonConventionService conventionService) : base(conventionService){}
        private PythonConventionService localConventions;
        public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
        {
            if(codeElement == null) throw new ArgumentNullException(nameof(codeElement));
            if(codeElement.ReturnType == null) throw new InvalidOperationException($"{nameof(codeElement.ReturnType)} should not be null");
            if(writer == null) throw new ArgumentNullException(nameof(writer));
            if(!(codeElement.Parent is CodeClass)) throw new InvalidOperationException("the parent of a method should be a class");

            localConventions = new PythonConventionService(writer); //because we allow inline type definitions for method parameters
            var returnType = localConventions.GetTypeString(codeElement.ReturnType, codeElement);
            var isVoid = "None".Equals(returnType, StringComparison.OrdinalIgnoreCase);
            WriteMethodPrototype(codeElement, writer, returnType, isVoid);
            writer.IncreaseIndent();
            WriteMethodDocumentation(codeElement, writer, returnType, isVoid);
            var parentClass = codeElement.Parent as CodeClass;
            var inherits = parentClass.StartBlock.Inherits != null && !parentClass.IsErrorDefinition;
            var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
            var requestConfigParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestConfiguration);
            var requestParams = new RequestParams(requestBodyParam, requestConfigParam);
            if(!codeElement.IsOfKind(CodeMethodKind.Setter))
                foreach(var parameter in codeElement.Parameters.Where(x => !x.Optional).OrderBy(x => x.Name)) {
                    var parameterName = parameter.Name.ToSnakeCase();
                    writer.WriteLine($"if not {parameterName}:");
                    writer.IncreaseIndent();
                    writer.WriteLine($"raise Exception(\"{parameterName} cannot be undefined\")");
                    writer.DecreaseIndent();
                }
            switch(codeElement.Kind) {
                case CodeMethodKind.ClientConstructor:
                    WriteConstructorBody(parentClass, codeElement, writer, inherits);
                    WriteApiConstructorBody(parentClass, codeElement, writer);
                break;
                case CodeMethodKind.Constructor:
                    WriteConstructorBody(parentClass, codeElement, writer, inherits);
                    break;
                case CodeMethodKind.IndexerBackwardCompatibility:
                    WriteIndexerBody(codeElement, parentClass, returnType, writer);
                    break;
                case CodeMethodKind.Deserializer:
                    WriteDeserializerBody(codeElement, parentClass, writer, inherits);
                    break;
                case CodeMethodKind.Serializer:
                    WriteSerializerBody(inherits, parentClass, writer);
                    break;
                case CodeMethodKind.RequestGenerator:
                    WriteRequestGeneratorBody(codeElement, requestParams, parentClass, writer);
                break;
                case CodeMethodKind.RequestExecutor:
                    WriteRequestExecutorBody(codeElement, requestParams, isVoid, returnType, writer);
                    break;
                case CodeMethodKind.Getter:
                    WriteGetterBody(codeElement, writer, parentClass);
                    break;
                case CodeMethodKind.Setter:
                    WriteSetterBody(codeElement, writer, parentClass);
                    break;
                case CodeMethodKind.RequestBuilderWithParameters:
                    WriteRequestBuilderWithParametersBody(codeElement, parentClass, returnType, writer);
                    break;
                case CodeMethodKind.QueryParametersMapper:
                    WriteQueryParametersMapper(codeElement, parentClass, writer);
                    break;
                case CodeMethodKind.RawUrlConstructor:
                    throw new InvalidOperationException("RawUrlConstructor is not supported as typescript relies on union types.");
                case CodeMethodKind.RequestBuilderBackwardCompatibility:
                    throw new InvalidOperationException("RequestBuilderBackwardCompatibility is not supported as the request builders are implemented by properties.");
                default:
                    WriteDefaultMethodBody(codeElement, writer, returnType);
                    break;
            }
            writer.DecreaseIndent();
            writer.WriteLine();
        }
        private void WriteIndexerBody(CodeMethod codeElement, CodeClass parentClass, string returnType, LanguageWriter writer) {
            var pathParametersProperty = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
            localConventions.AddParametersAssignment(writer, pathParametersProperty.Type, $"self.{pathParametersProperty.Name}",
                (codeElement.OriginalIndexer.IndexType, codeElement.OriginalIndexer.SerializationName, "id"));
            localConventions.AddRequestBuilderBody(parentClass, returnType, writer, conventions.TempDictionaryVarName);
        }
        private void WriteRequestBuilderWithParametersBody(CodeMethod codeElement, CodeClass parentClass, string returnType, LanguageWriter writer)
        {
            var codePathParameters = codeElement.Parameters
                                                        .Where(x => x.IsOfKind(CodeParameterKind.Path));
            localConventions.AddRequestBuilderBody(parentClass, returnType, writer, pathParameters: codePathParameters);
        }
        private static void WriteApiConstructorBody(CodeClass parentClass, CodeMethod method, LanguageWriter writer) {
            var requestAdapterProperty = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
            var backingStoreParameter = method.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.BackingStore));
            var requestAdapterPropertyName = requestAdapterProperty.Name.ToSnakeCase();
            WriteSerializationRegistration(method.SerializerModules, writer, "register_default_serializer");
            WriteSerializationRegistration(method.DeserializerModules, writer, "register_default_deserializer");
            writer.WriteLine($"if not {requestAdapterPropertyName}.base_url:");
            writer.IncreaseIndent();
            writer.WriteLine($"{requestAdapterPropertyName}.base_url = \"{method.BaseUrl}\"");
            writer.DecreaseIndent();
            if(backingStoreParameter != null)
                writer.WriteLine($"self.{requestAdapterPropertyName}.enable_backing_store({backingStoreParameter.Name})");
        }
        private static void WriteQueryParametersMapper(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
        {
            var parameter = codeElement.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.QueryParametersMapperParameter));
            if(parameter == null) throw new InvalidOperationException("QueryParametersMapper should have a parameter of type QueryParametersMapper");
            var parameterName = parameter.Name.ToSnakeCase();
            var escapedProperties = parentClass.Properties.Where(x => x.IsOfKind(CodePropertyKind.QueryParameter) && x.IsNameEscaped);
            foreach(var escapedProperty in escapedProperties) {
                writer.WriteLine($"if {parameterName} == \"{escapedProperty.Name}\":");
                writer.IncreaseIndent();
                writer.WriteLine($"return \"{escapedProperty.SerializationName}\"");
                writer.DecreaseIndent();
            }
            writer.WriteLine($"return {parameterName}");
        }
        private static void WriteSerializationRegistration(HashSet<string> serializationModules, LanguageWriter writer, string methodName) {
            if(serializationModules != null)
                foreach(var module in serializationModules)
                    writer.WriteLine($"{methodName}({module})");
        }
        private void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer, bool inherits) {
            if(inherits)
                writer.WriteLine("super().__init__()");
            var propertiesWithDefaultValues = new List<CodePropertyKind> {
                CodePropertyKind.AdditionalData,
                CodePropertyKind.BackingStore,
                CodePropertyKind.RequestBuilder,
                CodePropertyKind.UrlTemplate,
                CodePropertyKind.PathParameters,
            };
            foreach (var propWithoutDefault in parentClass.Properties.Except(parentClass.GetPropertiesOfKind(propertiesWithDefaultValues.ToArray()))
                                            .Except(parentClass.GetPropertiesOfKind(CodePropertyKind.RequestAdapter))
                                            .OrderByDescending(x => x.Kind)
                                            .ThenBy(x => x.Name)) {
                var returnType = conventions.GetTypeString(propWithoutDefault.Type, propWithoutDefault);
                conventions.WriteInLineDescription(propWithoutDefault.Description, writer);
                writer.WriteLine($"self.{conventions.GetAccessModifier(propWithoutDefault.Access)}{propWithoutDefault.NamePrefix}{propWithoutDefault.Name.ToSnakeCase()}: {(propWithoutDefault.Type.IsNullable ? "Optional[" : string.Empty)}{returnType}{(propWithoutDefault.Type.IsNullable ? "]" : string.Empty)} = None");
                writer.WriteLine();
            }
            foreach(var propWithDefault in parentClass.GetPropertiesOfKind(propertiesWithDefaultValues.ToArray())
                                            .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
                                            .OrderByDescending(x => x.Kind)
                                            .ThenBy(x => x.Name)) {
                var returnType = conventions.GetTypeString(propWithDefault.Type, propWithDefault);
                conventions.WriteInLineDescription(propWithDefault.Description, writer);
                writer.WriteLine($"self.{conventions.GetAccessModifier(propWithDefault.Access)}{propWithDefault.NamePrefix}{propWithDefault.Name.ToSnakeCase()}: {(propWithDefault.Type.IsNullable ? "Optional[" : string.Empty)}{returnType}{(propWithDefault.Type.IsNullable ? "]" : string.Empty)} = {propWithDefault.DefaultValue}");
                writer.WriteLine();
            }
            if(parentClass.IsOfKind(CodeClassKind.RequestBuilder)) {
                if(currentMethod.IsOfKind(CodeMethodKind.Constructor)) {
                    var pathParametersParam = currentMethod.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.PathParameters));
                    localConventions.AddParametersAssignment(writer, 
                                                        pathParametersParam.Type.AllTypes.OfType<CodeType>().FirstOrDefault(),
                                                        pathParametersParam.Name.ToFirstCharacterLowerCase(),
                                                        currentMethod.Parameters
                                                                    .Where(x => x.IsOfKind(CodeParameterKind.Path))
                                                                    .Select(x => (x.Type, x.SerializationName, x.Name.ToFirstCharacterLowerCase()))
                                                                    .ToArray());
                    AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.PathParameters, CodePropertyKind.PathParameters, writer, conventions.TempDictionaryVarName);
                }
                AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.RequestAdapter, CodePropertyKind.RequestAdapter, writer);
            }
        }
        private static void AssignPropertyFromParameter(CodeClass parentClass, CodeMethod currentMethod, CodeParameterKind parameterKind, CodePropertyKind propertyKind, LanguageWriter writer, string variableName = default) {
            var property = parentClass.GetPropertyOfKind(propertyKind);
            if(property != null) {
                var parameter = currentMethod.Parameters.FirstOrDefault(x => x.IsOfKind(parameterKind));
                if(!string.IsNullOrEmpty(variableName))
                    writer.WriteLine($"self.{property.Name.ToSnakeCase()} = {variableName.ToSnakeCase()}");
                else if(parameter != null)
                    writer.WriteLine($"self.{property.Name.ToSnakeCase()} = {parameter.Name.ToSnakeCase()}");
            }
        }
        private static void WriteSetterBody(CodeMethod codeElement, LanguageWriter writer, CodeClass parentClass) {
            var backingStore = parentClass.GetBackingStoreProperty();
            if(backingStore == null)
                writer.WriteLine($"self.{codeElement.AccessedProperty?.NamePrefix}{codeElement.AccessedProperty?.Name?.ToSnakeCase()} = value");
            else
                writer.WriteLine($"self.{backingStore.NamePrefix}{backingStore.Name.ToSnakeCase()}[\"{codeElement.AccessedProperty?.Name?.ToSnakeCase()}\"] = value");
        }
        private void WriteGetterBody(CodeMethod codeElement, LanguageWriter writer, CodeClass parentClass) {
            var backingStore = parentClass.GetBackingStoreProperty();
            if(backingStore == null)
                writer.WriteLine($"return self.{codeElement.AccessedProperty?.NamePrefix}{codeElement.AccessedProperty?.Name?.ToSnakeCase()}");
            else 
                if(!(codeElement.AccessedProperty?.Type?.IsNullable ?? true) &&
                    !(codeElement.AccessedProperty?.ReadOnly ?? true) &&
                    !string.IsNullOrEmpty(codeElement.AccessedProperty?.DefaultValue)) {
                    writer.WriteLines($"value: {conventions.GetTypeString(codeElement.AccessedProperty.Type, codeElement)} = self.{backingStore.NamePrefix}{backingStore.Name.ToSnakeCase()}.get(\"{codeElement.AccessedProperty.Name.ToSnakeCase()}\")",
                        "if not value:");
                    writer.IncreaseIndent();
                    writer.WriteLines($"value = {codeElement.AccessedProperty.DefaultValue}",
                        $"self.{codeElement.AccessedProperty?.NamePrefix}{codeElement.AccessedProperty?.Name?.ToSnakeCase()} = value");
                    writer.DecreaseIndent();
                    writer.WriteLines("return value");
                } else
                    writer.WriteLine($"return self.{backingStore.NamePrefix}{backingStore.Name.ToSnakeCase()}.get(\"{codeElement.AccessedProperty?.Name?.ToSnakeCase()}\")");

        }
        private static void WriteDefaultMethodBody(CodeMethod codeElement, LanguageWriter writer, string returnType) {
            var promisePrefix = codeElement.IsAsync ? "await " : string.Empty;
            writer.WriteLine($"return {promisePrefix}{returnType}()");
        }
        private void WriteDeserializerBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer, bool inherits) {
            writer.WriteLine("fields = {");
                writer.IncreaseIndent();
                foreach(var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)) {
                    writer.WriteLine($"\"{otherProp.SerializationName ?? otherProp.Name.ToSnakeCase()}\": lambda n : setattr(self, '{otherProp.Name.ToSnakeCase()}', n.{GetDeserializationMethodName(otherProp.Type, codeElement)}),");
                }
                writer.DecreaseIndent();
                writer.WriteLine("}");
                if (inherits) {
                    writer.WriteLine($"super_fields = super().{codeElement.Name.ToSnakeCase()}()");
                    writer.WriteLine($"fields.update(super_fields)");
                }
                writer.WriteLine("return fields");
        }
        private void WriteRequestExecutorBody(CodeMethod codeElement, RequestParams requestParams, bool isVoid, string returnType, LanguageWriter writer) {
            if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");

            var generatorMethodName = (codeElement.Parent as CodeClass)
                                                .Methods
                                                .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod)
                                                ?.Name
                                                ?.ToSnakeCase();
            writer.WriteLine($"request_info = self.{generatorMethodName}(");
            var requestInfoParameters = new CodeParameter[] { requestParams.requestBody, requestParams.requestConfiguration }
                	                        .Select(x => x?.Name.ToSnakeCase()).Where(x => x != null);
            if(requestInfoParameters.Any()) {
                writer.IncreaseIndent();
                writer.WriteLine(requestInfoParameters.Aggregate((x,y) => $"{x}, {y}"));
                writer.DecreaseIndent();
            }
            writer.WriteLine(")");
            var isStream = localConventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase);
            var returnTypeWithoutCollectionSymbol = GetReturnTypeWithoutCollectionSymbol(codeElement, returnType);
            var genericTypeForSendMethod = GetSendRequestMethodName(isVoid, isStream, codeElement.ReturnType.IsCollection, returnTypeWithoutCollectionSymbol);
            var newFactoryParameter = GetTypeFactory(isVoid, isStream, returnTypeWithoutCollectionSymbol);
            var errorMappingVarName = "None";
        if(codeElement.ErrorMappings.Any()) {
            errorMappingVarName = "error_mapping";
            writer.WriteLine($"{errorMappingVarName}: Dict[str, ParsableFactory] = {{");
            writer.IncreaseIndent();
            foreach(var errorMapping in codeElement.ErrorMappings) {
                writer.WriteLine($"\"{errorMapping.Key.ToUpperInvariant()}\": o_data_error.{errorMapping.Value.Name}.get_from_discriminator_value(),");
            }
            writer.CloseBlock("}");
        }
        writer.WriteLine($"if not self.request_adapter:");
        writer.IncreaseIndent();
        writer.WriteLine($"raise Exception(\"Http core is null\") ");
        writer.DecreaseIndent();
        writer.WriteLine($"return await self.request_adapter.{genericTypeForSendMethod}(request_info,{newFactoryParameter} response_handler, {errorMappingVarName})");
        }
        private string GetReturnTypeWithoutCollectionSymbol(CodeMethod codeElement, string fullTypeName) {
            if(!codeElement.ReturnType.IsCollection) return fullTypeName;
            var clone = codeElement.ReturnType.Clone() as CodeTypeBase;
            clone.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.None;
            return conventions.GetTypeString(clone, codeElement);
        }
        private const string RequestInfoVarName = "request_info";
        private void WriteRequestGeneratorBody(CodeMethod codeElement, RequestParams requestParams, CodeClass currentClass, LanguageWriter writer) {
            if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
            
            var urlTemplateParamsProperty = currentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
            var urlTemplateProperty = currentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate);
            var requestAdapterProperty = currentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
            writer.WriteLines($"{RequestInfoVarName} = RequestInformation()",
                                $"{RequestInfoVarName}.url_template = {GetPropertyCall(urlTemplateProperty, "''")}",
                                $"{RequestInfoVarName}.path_parameters = {GetPropertyCall(urlTemplateParamsProperty, "''")}",
                                $"{RequestInfoVarName}.http_method = Method.{codeElement.HttpMethod.ToString().ToUpperInvariant()}");
            if(requestParams.requestConfiguration != null) {
                writer.WriteLine($"if {requestParams.requestConfiguration.Name.ToSnakeCase()}:");
                writer.IncreaseIndent();
                var headers = requestParams.Headers;
                if(headers != null)
                    writer.WriteLine($"{RequestInfoVarName}.add_request_headers({requestParams.requestConfiguration.Name.ToSnakeCase()}.{headers.Name.ToSnakeCase()})");
                var queryString = requestParams.QueryParameters;
                if(queryString != null)
                    writer.WriteLines($"{RequestInfoVarName}.set_query_string_parameters_from_raw_object({requestParams.requestConfiguration.Name.ToSnakeCase()}.{queryString.Name.ToSnakeCase()})");
                var options = requestParams.Options;
                if(options != null)
                    writer.WriteLine($"{RequestInfoVarName}.add_request_options({requestParams.requestConfiguration.Name.ToSnakeCase()}.{options.Name.ToSnakeCase()})");
                writer.DecreaseIndent();
            }
            if(requestParams.requestBody != null) {
                if(requestParams.requestBody.Type.Name.Equals(localConventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
                    writer.WriteLine($"{RequestInfoVarName}.set_stream_content({requestParams.requestBody.Name.ToSnakeCase()})");
                else {
                    writer.WriteLine($"{RequestInfoVarName}.set_content_from_parsable(self.{requestAdapterProperty.Name.ToSnakeCase()}, \"{codeElement.RequestBodyContentType}\", {requestParams.requestBody.Name})");
                }
            }
            writer.WriteLine($"return {RequestInfoVarName}");
        }
        private static string GetPropertyCall(CodeProperty property, string defaultValue) => property == null ? defaultValue : $"self.{property.Name.ToSnakeCase()}";
        private void WriteSerializerBody(bool inherits, CodeClass parentClass, LanguageWriter writer) {
            var additionalDataProperty = parentClass.GetPropertyOfKind(CodePropertyKind.AdditionalData);
            if(inherits)
            writer.WriteLine("super().serialize(writer)");
            foreach(var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)) {
                writer.WriteLine($"writer.{GetSerializationMethodName(otherProp.Type)}(\"{otherProp.SerializationName ?? otherProp.Name}\", self.{otherProp.Name.ToSnakeCase()})");
            }
            if(additionalDataProperty != null)
                writer.WriteLine($"writer.write_additional_data_value(self.{additionalDataProperty.Name.ToSnakeCase()})");
        }
        private void WriteMethodDocumentation(CodeMethod code, LanguageWriter writer, string returnType, bool isVoid) {
            var isDescriptionPresent = !string.IsNullOrEmpty(code.Description);
            var parametersWithDescription = code.Parameters.Where(x => !string.IsNullOrEmpty(code.Description));
            var nullablePrefix = code.ReturnType.IsNullable && !isVoid ? "Optional[" : string.Empty;
            var nullableSuffix = code.ReturnType.IsNullable && !isVoid ? "]" : string.Empty;
            if (isDescriptionPresent || parametersWithDescription.Any()) {
                writer.WriteLine(localConventions.DocCommentStart);
                if(isDescriptionPresent)
                    writer.WriteLine($"{localConventions.DocCommentPrefix}{PythonConventionService.RemoveInvalidDescriptionCharacters(code.Description)}");
                if(parametersWithDescription.Any()) {
                    writer.WriteLine($"Args:");
                    writer.IncreaseIndent();
                    
                    foreach(var paramWithDescription in parametersWithDescription.OrderBy(x => x.Name))
                        writer.WriteLine($"{localConventions.DocCommentPrefix}{paramWithDescription.Name}: {PythonConventionService.RemoveInvalidDescriptionCharacters(paramWithDescription.Description)}");
                    writer.DecreaseIndent();
                }
                if(!isVoid)
                    writer.WriteLine($"{localConventions.DocCommentPrefix}Returns: {nullablePrefix}{returnType}{nullableSuffix}");
                writer.WriteLine(localConventions.DocCommentEnd);
            }
        }
        private static readonly PythonCodeParameterOrderComparer parameterOrderComparer = new();
        private void WriteMethodPrototype(CodeMethod code, LanguageWriter writer, string returnType, bool isVoid) {
            if (code.IsOfKind(CodeMethodKind.Factory))
                writer.WriteLine("@staticmethod");
            var accessModifier = localConventions.GetAccessModifier(code.Access);
            var isConstructor = code.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor);
            var methodName = (code.Kind switch {
                _ when code.IsAccessor => code.AccessedProperty?.Name,
                _ when isConstructor => "__init__",
                _ => code.Name,
            })?.ToSnakeCase();
            var asyncPrefix = code.IsAsync && code.Kind is CodeMethodKind.RequestExecutor ? "async ": string.Empty;
            var instanceReference = code.IsOfKind(CodeMethodKind.Factory) ? string.Empty: "self,";
            var parameters = string.Join(", ", code.Parameters.OrderBy(x => x, parameterOrderComparer).Select(p=> localConventions.GetParameterSignature(p, code)).ToList());
            var nullablePrefix = code.ReturnType.IsNullable && !isVoid ? "Optional[" : string.Empty;
            var nullableSuffix = code.ReturnType.IsNullable && !isVoid ? "]" : string.Empty;
            var propertyDecorator  = code.Kind switch {
                    CodeMethodKind.Getter => "@property",
                    CodeMethodKind.Setter => $"@{methodName}.setter",
                    _ => string.Empty
                };
            var nullReturnTypeSuffix = !isVoid && !isConstructor;
            var returnTypeSuffix = nullReturnTypeSuffix ? $"{nullablePrefix}{returnType}{nullableSuffix}" : "None";
            if (!string.IsNullOrEmpty(propertyDecorator))
                writer.WriteLine($"{propertyDecorator}");
            writer.WriteLine($"{asyncPrefix}def {accessModifier}{methodName}({instanceReference}{parameters}) -> {returnTypeSuffix}:");
        }
        private string GetDeserializationMethodName(CodeTypeBase propType, CodeMethod codeElement) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = localConventions.TranslateType(propType);
            if (localConventions.TypeExistInSameClassAsTarget(propType, codeElement))
                propertyType = codeElement.Parent.Name.ToFirstCharacterUpperCase();
            if(propType is CodeType currentType) {
                if(currentType.TypeDefinition is CodeEnum currentEnum)
                    return $"get_{(currentEnum.Flags || isCollection ? "collection_of_enum_values" : "enum_value")}({propertyType.ToCamelCase()})";
                else if(isCollection)
                    if(currentType.TypeDefinition == null)
                        return $"get_collection_of_primitive_values({propertyType.ToSnakeCase()})";
                    else
                        return $"get_collection_of_object_values({propertyType.ToCamelCase()})";
            }
            return propertyType switch
            {
                "str" or "bool" or "int" or "float" or "UUID" or "date" or "time" or "datetime" or "timedelta" => $"get_{propertyType.ToSnakeCase()}_value()",
                "bytes" => "get_byte_array_value()",
                _ => $"get_object_value({propertyType.ToCamelCase()})",
            };
        }
        private string GetSerializationMethodName(CodeTypeBase propType) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = localConventions.TranslateType(propType);
            if(propType is CodeType currentType) {
                if(currentType.TypeDefinition is CodeEnum)
                    return $"write_enum_value";
                else if(isCollection)
                    if(currentType.TypeDefinition == null)
                        return $"write_collection_of_primitive_values";
                    else
                        return $"write_collection_of_object_values";
            }
            return propertyType switch
            {
                "str" or "bool" or "int" or "float" or "UUID" or "date" or "time" or "datetime" or "timedelta" => $"write_{propertyType.ToSnakeCase()}_value",
                _ => $"write_object_value",
            };
        }
        private string GetTypeFactory(bool isVoid, bool isStream, string returnType) {
            if(isVoid) return string.Empty;
            else if(isStream || conventions.IsPrimitiveType(returnType)) return $" \"{returnType}\",";
            else return $" {returnType},";
        }
        private string GetSendRequestMethodName(bool isVoid, bool isStream, bool isCollection, string returnType) {
            if(isVoid) return "send_no_response_content_async";
            else if(isCollection) {
                if(conventions.IsPrimitiveType(returnType)) return $"send_collection_of_primitive_async";
                else return $"send_collection_async({returnType})";
            }
            else if(isStream || conventions.IsPrimitiveType(returnType)) return $"send_primitive_async";
            else return $"send_async";
        }
    }
}
