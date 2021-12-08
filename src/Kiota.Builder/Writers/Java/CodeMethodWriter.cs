using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Extensions;

namespace Kiota.Builder.Writers.Java {
    public class CodeMethodWriter : BaseElementWriter<CodeMethod, JavaConventionService>
    {
        public CodeMethodWriter(JavaConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
        {
            if(codeElement == null) throw new ArgumentNullException(nameof(codeElement));
            if(codeElement.ReturnType == null) throw new InvalidOperationException($"{nameof(codeElement.ReturnType)} should not be null");
            if(writer == null) throw new ArgumentNullException(nameof(writer));
            if(!(codeElement.Parent is CodeClass)) throw new InvalidOperationException("the parent of a method should be a class");

            var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement);
            WriteMethodDocumentation(codeElement, writer);
            if(returnType.Equals("void", StringComparison.OrdinalIgnoreCase))
            {
                if(codeElement.IsOfKind(CodeMethodKind.RequestExecutor))
                    returnType = "Void"; //generic type for the future
            } else if(!codeElement.IsAsync)
                writer.WriteLine(codeElement.ReturnType.IsNullable && !codeElement.IsAsync ? "@javax.annotation.Nullable" : "@javax.annotation.Nonnull");
            WriteMethodPrototype(codeElement, writer, returnType);
            writer.IncreaseIndent();
            var parentClass = codeElement.Parent as CodeClass;
            var inherits = (parentClass.StartBlock as CodeClass.Declaration).Inherits != null;
            var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
            var queryStringParam = codeElement.Parameters.OfKind(CodeParameterKind.QueryParameter);
            var headersParam = codeElement.Parameters.OfKind(CodeParameterKind.Headers);
            var optionsParam = codeElement.Parameters.OfKind(CodeParameterKind.Options);
            var requestParams = new RequestParams(requestBodyParam, queryStringParam, headersParam, optionsParam);
            AddNullChecks(codeElement, writer);
            switch(codeElement.MethodKind) {
                case CodeMethodKind.Serializer:
                    WriteSerializerBody(parentClass, codeElement, writer);
                break;
                case CodeMethodKind.Deserializer:
                    WriteDeserializerBody(codeElement, codeElement, parentClass, writer);
                break;
                case CodeMethodKind.IndexerBackwardCompatibility:
                    WriteIndexerBody(codeElement, parentClass, writer, returnType);
                break;
                case CodeMethodKind.RequestGenerator when codeElement.IsOverload:
                    WriteGeneratorMethodCall(codeElement, requestParams, writer, "return ");
                break;
                case CodeMethodKind.RequestGenerator when !codeElement.IsOverload:
                    WriteRequestGeneratorBody(codeElement, requestParams, parentClass, writer);
                break;
                case CodeMethodKind.RequestExecutor:
                    WriteRequestExecutorBody(codeElement, requestParams, writer);
                break;
                case CodeMethodKind.Getter:
                    WriteGetterBody(codeElement, writer, parentClass);
                    break;
                case CodeMethodKind.Setter:
                    WriteSetterBody(codeElement, writer, parentClass);
                    break;
                case CodeMethodKind.ClientConstructor:
                    WriteConstructorBody(parentClass, codeElement, writer, inherits);
                    WriteApiConstructorBody(parentClass, codeElement, writer);
                break;
                case CodeMethodKind.Constructor when codeElement.IsOverload && parentClass.IsOfKind(CodeClassKind.RequestBuilder):
                    WriteRequestBuilderConstructorCall(codeElement, writer);
                break;
                case CodeMethodKind.Constructor:
                case CodeMethodKind.RawUrlConstructor:
                    WriteConstructorBody(parentClass, codeElement, writer, inherits);
                    break;
                case CodeMethodKind.RequestBuilderWithParameters:
                    WriteRequestBuilderBody(parentClass, codeElement, writer);
                    break;
                case CodeMethodKind.RequestBuilderBackwardCompatibility:
                    throw new InvalidOperationException("RequestBuilderBackwardCompatibility is not supported as the request builders are implemented by properties.");
                default:
                    writer.WriteLine("return null;");
                break;
            }
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
        private void WriteRequestBuilderBody(CodeClass parentClass, CodeMethod codeElement, LanguageWriter writer)
        {
            var importSymbol = conventions.GetTypeString(codeElement.ReturnType, parentClass);
            conventions.AddRequestBuilderBody(parentClass, importSymbol, writer, pathParameters: codeElement.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Path)));
        }
        private static void AddNullChecks(CodeMethod codeElement, LanguageWriter writer) {
            if(!codeElement.IsOverload)
                foreach(var parameter in codeElement.Parameters.Where(x => !x.Optional).OrderBy(x => x.Name))
                    writer.WriteLine($"Objects.requireNonNull({parameter.Name.ToFirstCharacterLowerCase()});");
        }
        private static void WriteRequestBuilderConstructorCall(CodeMethod codeElement, LanguageWriter writer)
        {
            var requestAdapterParameter = codeElement.Parameters.OfKind(CodeParameterKind.RequestAdapter);
            var urlTemplateParamsParameter = codeElement.Parameters.OfKind(CodeParameterKind.PathParameters);
            var pathParameters = codeElement.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Path));
            var pathParametersRef = pathParameters.Any() ? (", " + pathParameters.Select(x => x.Name).Aggregate((x, y) => $"{x}, {y}")) : string.Empty;
            writer.WriteLine($"this({urlTemplateParamsParameter.Name}, {requestAdapterParameter.Name}{pathParametersRef});");
        }
        private static void WriteApiConstructorBody(CodeClass parentClass, CodeMethod method, LanguageWriter writer) {
            var requestAdapterProperty = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
            var backingStoreParameter = method.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.BackingStore));
            var requestAdapterPropertyName = requestAdapterProperty.Name.ToFirstCharacterLowerCase();
            WriteSerializationRegistration(method.SerializerModules, writer, "registerDefaultSerializer");
            WriteSerializationRegistration(method.DeserializerModules, writer, "registerDefaultDeserializer");
            writer.WriteLine($"{requestAdapterPropertyName}.setBaseUrl(\"{method.BaseUrl}\");");
            if(backingStoreParameter != null)
                writer.WriteLine($"this.{requestAdapterPropertyName}.enableBackingStore({backingStoreParameter.Name});");
        }
        private static void WriteSerializationRegistration(List<string> serializationModules, LanguageWriter writer, string methodName) {
            if(serializationModules != null)
                foreach(var module in serializationModules)
                    writer.WriteLine($"ApiClientBuilder.{methodName}({module}.class);");
        }
        private void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer, bool inherits) {
            if(inherits)
                writer.WriteLine("super();");
            foreach(var propWithDefault in parentClass.GetPropertiesOfKind(CodePropertyKind.BackingStore,
                                                                            CodePropertyKind.RequestBuilder,
                                                                            CodePropertyKind.UrlTemplate,
                                                                            CodePropertyKind.PathParameters)
                                            .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
                                            .OrderBy(x => x.Name)) {
                writer.WriteLine($"this.{propWithDefault.NamePrefix}{propWithDefault.Name.ToFirstCharacterLowerCase()} = {propWithDefault.DefaultValue};");
            }
            foreach(var propWithDefault in parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData) //additional data and backing Store rely on accessors
                                            .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
                                            .OrderBy(x => x.Name)) {
                writer.WriteLine($"this.set{propWithDefault.Name.ToFirstCharacterUpperCase()}({propWithDefault.DefaultValue});");
            }
            if(parentClass.IsOfKind(CodeClassKind.RequestBuilder)) {
                if(currentMethod.IsOfKind(CodeMethodKind.Constructor)) {
                    var pathParametersParam = currentMethod.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.PathParameters));
                    conventions.AddParametersAssignment(writer, 
                                                        pathParametersParam.Type,
                                                        pathParametersParam.Name.ToFirstCharacterLowerCase(),
                                                        currentMethod.Parameters
                                                                    .Where(x => x.IsOfKind(CodeParameterKind.Path))
                                                                    .Select(x => (x.Type, x.UrlTemplateParameterName, x.Name.ToFirstCharacterLowerCase()))
                                                                    .ToArray());
                    AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.PathParameters, CodePropertyKind.PathParameters, writer, conventions.TempDictionaryVarName);
                }
                else if(currentMethod.IsOfKind(CodeMethodKind.RawUrlConstructor)) {
                    var pathParametersProp = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
                    var rawUrlParam = currentMethod.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.RawUrl));
                    conventions.AddParametersAssignment(writer,
                                                        pathParametersProp.Type,
                                                        string.Empty,
                                                        (rawUrlParam.Type, Constants.RawUrlParameterName, rawUrlParam.Name.ToFirstCharacterLowerCase()));
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
                    writer.WriteLine($"this.{property.Name.ToFirstCharacterLowerCase()} = {variableName};");
                else if (parameter != null)
                    writer.WriteLine($"this.{property.Name.ToFirstCharacterLowerCase()} = {parameter.Name};");
            }
        }
        private static void WriteSetterBody(CodeMethod codeElement, LanguageWriter writer, CodeClass parentClass) {
            var backingStore = parentClass.GetBackingStoreProperty();
            if(backingStore == null)
                writer.WriteLine($"this.{codeElement.AccessedProperty?.NamePrefix}{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()} = value;");
            else
                writer.WriteLine($"this.get{backingStore.Name.ToFirstCharacterUpperCase()}().set(\"{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()}\", value);");
        }
        private void WriteGetterBody(CodeMethod codeElement, LanguageWriter writer, CodeClass parentClass) {
            var backingStore = parentClass.GetBackingStoreProperty();
            if(backingStore == null || (codeElement.AccessedProperty?.IsOfKind(CodePropertyKind.BackingStore) ?? false))
                writer.WriteLine($"return this.{codeElement.AccessedProperty?.NamePrefix}{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()};");
            else 
                if(!(codeElement.AccessedProperty?.Type?.IsNullable ?? true) &&
                   !(codeElement.AccessedProperty?.ReadOnly ?? true) &&
                    !string.IsNullOrEmpty(codeElement.AccessedProperty?.DefaultValue)) {
                    writer.WriteLines($"{conventions.GetTypeString(codeElement.AccessedProperty.Type, codeElement)} value = this.{backingStore.NamePrefix}{backingStore.Name.ToFirstCharacterLowerCase()}.get(\"{codeElement.AccessedProperty.Name.ToFirstCharacterLowerCase()}\");",
                        "if(value == null) {");
                    writer.IncreaseIndent();
                    writer.WriteLines($"value = {codeElement.AccessedProperty.DefaultValue};",
                        $"this.set{codeElement.AccessedProperty?.Name?.ToFirstCharacterUpperCase()}(value);");
                    writer.DecreaseIndent();
                    writer.WriteLines("}", "return value;");
                } else
                    writer.WriteLine($"return this.get{backingStore.Name.ToFirstCharacterUpperCase()}().get(\"{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()}\");");

        }
        private void WriteIndexerBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer, string returnType) {
            var pathParametersProperty = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
            conventions.AddParametersAssignment(writer, pathParametersProperty.Type, $"this.{pathParametersProperty.Name}",
                (codeElement.OriginalIndexer.IndexType, codeElement.OriginalIndexer.ParameterName, "id"));
            conventions.AddRequestBuilderBody(parentClass, returnType, writer, conventions.TempDictionaryVarName);
        }
        private void WriteDeserializerBody(CodeMethod codeElement, CodeMethod method, CodeClass parentClass, LanguageWriter writer) {
            var inherits = (parentClass.StartBlock as CodeClass.Declaration).Inherits != null;
            var fieldToSerialize = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom);
            writer.WriteLine($"return new HashMap<>({(inherits ? "super." + codeElement.Name.ToFirstCharacterLowerCase()+ "()" : fieldToSerialize.Count())}) {{{{");
            if(fieldToSerialize.Any()) {
                writer.IncreaseIndent();
                fieldToSerialize
                        .OrderBy(x => x.Name)
                        .Select(x => {
                            var setterName = x.IsNameEscaped && !string.IsNullOrEmpty(x.SerializationName) ? x.SerializationName : x.Name;
                            return $"this.put(\"{x.SerializationName ?? x.Name.ToFirstCharacterLowerCase()}\", (o, n) -> {{ (({parentClass.Name.ToFirstCharacterUpperCase()})o).set{setterName.ToFirstCharacterUpperCase()}({GetDeserializationMethodName(x.Type, method)}); }});";
                        })
                        .ToList()
                        .ForEach(x => writer.WriteLine(x));
                writer.DecreaseIndent();
            }
            writer.WriteLine("}};");
        }
        private void WriteRequestExecutorBody(CodeMethod codeElement, RequestParams requestParams, LanguageWriter writer) {
            if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
            var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement, false);
            writer.WriteLine("try {");
            writer.IncreaseIndent();
            WriteGeneratorMethodCall(codeElement, requestParams, writer, $"final RequestInformation {RequestInfoVarName} = ");
            var sendMethodName = GetSendRequestMethodName(codeElement.ReturnType.IsCollection, returnType);
            var responseHandlerParam = codeElement.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.ResponseHandler));
            if(codeElement.Parameters.Any(x => x.IsOfKind(CodeParameterKind.ResponseHandler)))
                writer.WriteLine($"return this.requestAdapter.{sendMethodName}({RequestInfoVarName}, {returnType}.class, {responseHandlerParam.Name});");
            else
                writer.WriteLine($"return this.requestAdapter.{sendMethodName}({RequestInfoVarName}, {returnType}.class, null);");
            writer.DecreaseIndent();
            writer.WriteLine("} catch (URISyntaxException ex) {");
            writer.IncreaseIndent();
            writer.WriteLine("return java.util.concurrent.CompletableFuture.failedFuture(ex);");
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
        private string GetSendRequestMethodName(bool isCollection, string returnType) {
            if(conventions.PrimitiveTypes.Contains(returnType)) 
                if(isCollection)
                    return "sendPrimitiveCollectionAsync";
                else
                    return $"sendPrimitiveAsync";
            else if(isCollection) return $"sendCollectionAsync";
            else return $"sendAsync";
        }
        private const string RequestInfoVarName = "requestInfo";
        private static void WriteGeneratorMethodCall(CodeMethod codeElement, RequestParams requestParams, LanguageWriter writer, string prefix) {
            var generatorMethodName = (codeElement.Parent as CodeClass)
                                                .Methods
                                                .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod)
                                                ?.Name
                                                ?.ToFirstCharacterLowerCase();
            var paramsList = new CodeParameter[] { requestParams.requestBody, requestParams.queryString, requestParams.headers, requestParams.options };
            var requestInfoParameters = paramsList.Where(x => x != null)
                                                .Select(x => x.Name)
                                                .ToList();
            var skipIndex = requestParams.requestBody == null ? 1 : 0;
            if(codeElement.IsOverload && !codeElement.OriginalMethod.Parameters.Any(x => x.IsOfKind(CodeParameterKind.QueryParameter)) || // we're on an overload and the original method has no query parameters
                !codeElement.IsOverload && requestParams.queryString == null) // we're on the original method and there is no query string parameter
                skipIndex++;// we skip the query string parameter null value
            requestInfoParameters.AddRange(paramsList.Where(x => x == null).Skip(skipIndex).Select(x => "null"));
            var paramsCall = requestInfoParameters.Any() ? requestInfoParameters.Aggregate((x,y) => $"{x}, {y}") : string.Empty;
            writer.WriteLine($"{prefix}{generatorMethodName}({paramsCall});");
        }
        private void WriteRequestGeneratorBody(CodeMethod codeElement, RequestParams requestParams, CodeClass currentClass, LanguageWriter writer) {
            if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
            
            var urlTemplateParamsProperty = currentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
            var urlTemplateProperty = currentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate);
            var requestAdapterProperty = currentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
            writer.WriteLine($"final RequestInformation {RequestInfoVarName} = new RequestInformation() {{{{");
            writer.IncreaseIndent();
            writer.WriteLine($"httpMethod = HttpMethod.{codeElement.HttpMethod?.ToString().ToUpperInvariant()};");
            writer.DecreaseIndent();
            writer.WriteLine("}};");
            writer.WriteLines($"{RequestInfoVarName}.urlTemplate = {GetPropertyCall(urlTemplateProperty, "\"\"")};",
                            $"{RequestInfoVarName}.pathParameters = {GetPropertyCall(urlTemplateParamsProperty, "null")};");
            if(requestParams.requestBody != null)
                if(requestParams.requestBody.Type.Name.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
                    writer.WriteLine($"{RequestInfoVarName}.setStreamContent({requestParams.requestBody.Name});");
                else
                    writer.WriteLine($"{RequestInfoVarName}.setContentFromParsable({requestAdapterProperty.Name.ToFirstCharacterLowerCase()}, \"{codeElement.ContentType}\", {requestParams.requestBody.Name});");
            if(requestParams.queryString != null) {
                var httpMethodPrefix = codeElement.HttpMethod.ToString().ToFirstCharacterUpperCase();
                writer.WriteLine($"if ({requestParams.queryString.Name} != null) {{");
                writer.IncreaseIndent();
                writer.WriteLines($"final {httpMethodPrefix}QueryParameters qParams = new {httpMethodPrefix}QueryParameters();",
                            $"{requestParams.queryString.Name}.accept(qParams);",
                            $"qParams.AddQueryParameters({RequestInfoVarName}.queryParameters);");
                writer.DecreaseIndent();
                writer.WriteLine("}");
            }
            if(requestParams.headers != null) {
                writer.WriteLine($"if ({requestParams.headers.Name} != null) {{");
                writer.IncreaseIndent();
                writer.WriteLine($"{requestParams.headers.Name}.accept({RequestInfoVarName}.headers);");
                writer.DecreaseIndent();
                writer.WriteLine("}");
            }
            if(requestParams.options != null) {
                writer.WriteLine($"if ({requestParams.options.Name} != null) {{");
                writer.IncreaseIndent();
                writer.WriteLine($"{RequestInfoVarName}.addRequestOptions({requestParams.options.Name}.toArray(new RequestOption[0]));");
                writer.DecreaseIndent();
                writer.WriteLine("}");
            }
            writer.WriteLine($"return {RequestInfoVarName};");
        }
        private static string GetPropertyCall(CodeProperty property, string defaultValue) => property == null ? defaultValue : $"{property.Name.ToFirstCharacterLowerCase()}";
        private void WriteSerializerBody(CodeClass parentClass, CodeMethod method, LanguageWriter writer) {
            var additionalDataProperty = parentClass.GetPropertyOfKind(CodePropertyKind.AdditionalData);
            if((parentClass.StartBlock as CodeClass.Declaration).Inherits != null)
                writer.WriteLine("super.serialize(writer);");
            foreach(var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)) {
                var accessorName = otherProp.IsNameEscaped && !string.IsNullOrEmpty(otherProp.SerializationName) ? otherProp.SerializationName : otherProp.Name.ToFirstCharacterLowerCase();
                writer.WriteLine($"writer.{GetSerializationMethodName(otherProp.Type, method)}(\"{otherProp.SerializationName ?? otherProp.Name.ToFirstCharacterLowerCase()}\", this.get{accessorName.ToFirstCharacterUpperCase()}());");
            }
            if(additionalDataProperty != null)
                writer.WriteLine($"writer.writeAdditionalData(this.get{additionalDataProperty.Name.ToFirstCharacterUpperCase()}());");
        }
        private static readonly CodeParameterOrderComparer parameterOrderComparer = new();
        private void WriteMethodPrototype(CodeMethod code, LanguageWriter writer, string returnType) {
            var accessModifier = conventions.GetAccessModifier(code.Access);
            var genericTypeParameterDeclaration = code.IsOfKind(CodeMethodKind.Deserializer) ? " <T>": string.Empty;
            var returnTypeAsyncPrefix = code.IsAsync ? "java.util.concurrent.CompletableFuture<" : string.Empty;
            var returnTypeAsyncSuffix = code.IsAsync ? ">" : string.Empty;
            var isConstructor = code.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor, CodeMethodKind.RawUrlConstructor);
            var methodName = code.MethodKind switch {
                CodeMethodKind.Getter when (code.AccessedProperty?.IsNameEscaped ?? false) && !string.IsNullOrEmpty(code.AccessedProperty?.SerializationName)
                    => $"get{code.AccessedProperty.SerializationName.ToFirstCharacterUpperCase()}",
                CodeMethodKind.Setter when (code.AccessedProperty?.IsNameEscaped ?? false) && !string.IsNullOrEmpty(code.AccessedProperty?.SerializationName)
                    => $"set{code.AccessedProperty.SerializationName.ToFirstCharacterUpperCase()}",
                CodeMethodKind.Getter => $"get{code.AccessedProperty?.Name?.ToFirstCharacterUpperCase()}",
                CodeMethodKind.Setter => $"set{code.AccessedProperty?.Name?.ToFirstCharacterUpperCase()}",
                _ when isConstructor => code.Parent.Name.ToFirstCharacterUpperCase(),
                _ => code.Name.ToFirstCharacterLowerCase()
            };
            var parameters = string.Join(", ", code.Parameters.OrderBy(x => x, parameterOrderComparer).Select(p=> conventions.GetParameterSignature(p, code)).ToList());
            var throwableDeclarations = code.IsOfKind(CodeMethodKind.RequestGenerator) ? "throws URISyntaxException ": string.Empty;
            var collectionCorrectedReturnType = code.ReturnType.IsArray && code.IsOfKind(CodeMethodKind.RequestExecutor) ?
                                                $"Iterable<{returnType.StripArraySuffix()}>" :
                                                returnType;
            var finalReturnType = isConstructor ? string.Empty : $" {returnTypeAsyncPrefix}{collectionCorrectedReturnType}{returnTypeAsyncSuffix}";
            writer.WriteLine($"{accessModifier}{genericTypeParameterDeclaration}{finalReturnType} {methodName}({parameters}) {throwableDeclarations}{{");
        }
        private void WriteMethodDocumentation(CodeMethod code, LanguageWriter writer) {
            var isDescriptionPresent = !string.IsNullOrEmpty(code.Description);
            var parametersWithDescription = code.Parameters.Where(x => !string.IsNullOrEmpty(code.Description));
            if (isDescriptionPresent || parametersWithDescription.Any()) {
                writer.WriteLine(conventions.DocCommentStart);
                if(isDescriptionPresent)
                    writer.WriteLine($"{conventions.DocCommentPrefix}{JavaConventionService.RemoveInvalidDescriptionCharacters(code.Description)}");
                foreach(var paramWithDescription in parametersWithDescription.OrderBy(x => x.Name))
                    writer.WriteLine($"{conventions.DocCommentPrefix}@param {paramWithDescription.Name} {JavaConventionService.RemoveInvalidDescriptionCharacters(paramWithDescription.Description)}");
                
                if(code.IsAsync)
                    writer.WriteLine($"{conventions.DocCommentPrefix}@return a CompletableFuture of {code.ReturnType.Name}");
                else
                    writer.WriteLine($"{conventions.DocCommentPrefix}@return a {code.ReturnType.Name}");
                writer.WriteLine(conventions.DocCommentEnd);
            }
        }
        private string GetDeserializationMethodName(CodeTypeBase propType, CodeMethod method) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = conventions.GetTypeString(propType, method, false);
            if(propType is CodeType currentType) {
                if(isCollection)
                    if(currentType.TypeDefinition == null)
                        return $"n.getCollectionOfPrimitiveValues({propertyType.ToFirstCharacterUpperCase()}.class)";
                    else if (currentType.TypeDefinition is CodeEnum enumType)
                        return $"n.getCollectionOfEnumValues({enumType.Name.ToFirstCharacterUpperCase()}.class)";
                    else
                        return $"n.getCollectionOfObjectValues({propertyType.ToFirstCharacterUpperCase()}.class)";
                else if (currentType.TypeDefinition is CodeEnum currentEnum)
                    return $"n.getEnum{(currentEnum.Flags ? "Set" : string.Empty)}Value({propertyType.ToFirstCharacterUpperCase()}.class)";
            }
            return propertyType switch
            {
                "byte[]" => "n.getByteArrayValue()",
                "String" or "Boolean" or "Integer" or "Float" or "Long" or "Guid" or "OffsetDateTime" or "Double" => $"n.get{propertyType}Value()",
                _ => $"n.getObjectValue({propertyType.ToFirstCharacterUpperCase()}.class)",
            };
        }
        private string GetSerializationMethodName(CodeTypeBase propType, CodeMethod method) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = conventions.GetTypeString(propType, method, false);
            if(propType is CodeType currentType) {
                if(isCollection)
                    if(currentType.TypeDefinition == null)
                        return $"writeCollectionOfPrimitiveValues";
                    else if(currentType.TypeDefinition is CodeEnum)
                        return $"writeCollectionOfEnumValues";
                    else
                        return $"writeCollectionOfObjectValues";
                else if (currentType.TypeDefinition is CodeEnum currentEnum)
                    return $"writeEnum{(currentEnum.Flags ? "Set" : string.Empty)}Value";
            }
            return propertyType switch
            {
                "byte[]" => "writeByteArrayValue",
                "String" or "Boolean" or "Integer" or "Float" or "Long" or "Guid" or "OffsetDateTime" or "Double" => $"write{propertyType}Value",
                _ => $"writeObjectValue",
            };
        }
    }
}
