using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Extensions;

namespace Kiota.Builder.Writers.Php
{
    public class CodeMethodWriter: BaseElementWriter<CodeMethod, PhpConventionService>
    {
        public CodeMethodWriter(PhpConventionService conventionService) : base(conventionService) { }
        
        private const string RequestInfoVarName = "$requestInfo";
        public override void  WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
        {

            var parentClass = codeElement.Parent as CodeClass;
            var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement);
            var inherits = (parentClass?.StartBlock as CodeClass.Declaration)?.Inherits != null;
            var orNullReturn = codeElement.ReturnType.IsNullable ? new[]{"?", "|null"} : new[] {string.Empty, string.Empty};
            var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
            var queryStringParam = codeElement.Parameters.OfKind(CodeParameterKind.QueryParameter);
            var headersParam = codeElement.Parameters.OfKind(CodeParameterKind.Headers);
            var optionsParam = codeElement.Parameters.OfKind(CodeParameterKind.Options);
            var requestParams = new RequestParams(requestBodyParam, queryStringParam, headersParam, optionsParam);
            
            WriteMethodPhpDocs(codeElement, writer, orNullReturn);
            WriteMethodsAndParameters(codeElement, writer, orNullReturn, codeElement.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor));

            switch (codeElement.MethodKind)
            {
                    case CodeMethodKind.Constructor: 
                        WriteConstructorBody(parentClass, codeElement, writer, inherits);
                        break;
                    case CodeMethodKind.Serializer:
                        WriteSerializerBody(codeElement, parentClass, writer, inherits);
                        break;
                    case CodeMethodKind.Setter:
                        WriteSetterBody(writer, codeElement);
                        break;
                    case CodeMethodKind.Getter:
                        WriteGetterBody(writer, codeElement);
                        break;
                    case CodeMethodKind.Deserializer:
                        WriteDeserializerBody(parentClass, writer, codeElement);
                        break;
                    case CodeMethodKind.RequestBuilderWithParameters:
                        WriteRequestBuilderWithParametersBody(returnType, writer, codeElement);
                        break;
                    case CodeMethodKind.RequestGenerator:
                        WriteRequestGeneratorBody(codeElement, requestParams, parentClass, writer);
                        break;
                    case CodeMethodKind.ClientConstructor:
                        WriteConstructorBody(parentClass, codeElement, writer, inherits);
                        WriteApiConstructorBody(parentClass, codeElement, writer);
                        break;
                    case CodeMethodKind.IndexerBackwardCompatibility:
                        WriteIndexerBody(codeElement, parentClass, returnType, writer);
                        break;
                    case CodeMethodKind.RequestExecutor:
                        WriteRequestExecutorBody(codeElement, parentClass, requestParams, writer);
                        break;
            }
            writer.CloseBlock();
            writer.WriteLine();
        }
        
        private static void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer, bool inherits) {
            if(inherits)
                writer.WriteLine("parent::__construct();");
            foreach(var propWithDefault in parentClass.GetPropertiesOfKind(
                                                                            CodePropertyKind.BackingStore,
                                                                            CodePropertyKind.RequestBuilder,
                                                                            CodePropertyKind.UrlTemplate,
                                                                            CodePropertyKind.PathParameters)
                                            .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
                                            .OrderByDescending(x => x.PropertyKind)
                                            .ThenBy(x => x.Name))
            {
                var isPathSegment = propWithDefault.IsOfKind(CodePropertyKind.PathParameters);
                writer.WriteLine($"$this->{propWithDefault.NamePrefix}{propWithDefault.Name.ToFirstCharacterLowerCase()} = {(isPathSegment ? "[]" :propWithDefault.DefaultValue.ReplaceDoubleQuoteWithSingleQuote())};");
            }
            foreach(var propWithDefault in parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData) //additional data and backing Store rely on accessors
                .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
                .OrderBy(x => x.Name)) {
                writer.WriteLine($"$this->{propWithDefault.Name.ToFirstCharacterLowerCase()} = {propWithDefault.DefaultValue};");
            }
            if(currentMethod.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor)) {
                AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.RequestAdapter, CodePropertyKind.RequestAdapter, writer);
                AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.PathParameters, CodePropertyKind.PathParameters, writer);
                AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.RawUrl, CodePropertyKind.UrlTemplate, writer);
            }
            var pathParametersProperty = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
            if (currentMethod.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor) &&
                parentClass.IsOfKind(CodeClassKind.RequestBuilder) &&
                currentMethod.Parameters.Any(x => x.IsOfKind(CodeParameterKind.Path)))
            {
                writer.WriteLine($"$urlTplParams = $pathParameters;");
                currentMethod.Parameters.Where(parameter => parameter.IsOfKind(CodeParameterKind.Path)).ToList()
                    .ForEach(parameter =>
                    {
                        writer.WriteLine($"$urlTplParams['{parameter.Name}'] = ${parameter.Name};");
                    });
                writer.WriteLine($"{GetPropertyCall(pathParametersProperty, "[]")} = array_merge({GetPropertyCall(pathParametersProperty, "[]")}, $urlTplParams);");
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
                StringComparison.OrdinalIgnoreCase) && !codeMethod.IsOfKind(CodeMethodKind.RequestExecutor);
            if(hasMethodDescription){
                writer.WriteLine(
                    $"{conventions.DocCommentPrefix}{methodDescription}");
            }

            var accessedProperty = codeMethod.AccessedProperty;
            var isSetterForAdditionalData = (codeMethod.IsOfKind(CodeMethodKind.Setter) &&
                                             accessedProperty.IsOfKind(CodePropertyKind.AdditionalData));
            
            withDescription.Select(x => GetParameterDocString(codeMethod, x, isSetterForAdditionalData))
                .ToList()
                .ForEach(x => writer.WriteLine(x));
            var returnDocString = GetDocCommentReturnType(codeMethod, accessedProperty);
            if (!isVoidable)
            {
                writer.WriteLine((codeMethod.MethodKind == CodeMethodKind.RequestExecutor)
                    ? $"{conventions.DocCommentPrefix}@return Promise"
                    : $"{conventions.DocCommentPrefix}@return {returnDocString}{orNullReturn[1]}");
            }
            writer.WriteLine(conventions.DocCommentEnd);
        }

        private string GetDocCommentReturnType(CodeMethod codeMethod, CodeProperty accessedProperty)
        {
            return codeMethod.MethodKind switch
            {
                CodeMethodKind.Deserializer => "array<string, callable>",
                CodeMethodKind.Getter when accessedProperty.IsOfKind(CodePropertyKind.AdditionalData) => "array<string, mixed>",
                CodeMethodKind.Getter when accessedProperty.Type.IsArray || accessedProperty.Type.IsCollection => $"array<{conventions.TranslateType(accessedProperty.Type)}>",
                _ => conventions.GetTypeString(codeMethod.ReturnType, codeMethod)
            };
        }

        private string GetParameterDocString(CodeMethod codeMethod, CodeParameter x, bool isSetterForAdditionalData = false)
        {
            return codeMethod.MethodKind switch
            {
                CodeMethodKind.Setter => $"{conventions.DocCommentPrefix} @param {(isSetterForAdditionalData ? "array<string,mixed> $value": conventions.GetParameterDocNullable(x, x))} {x?.Description}",
                _ => $"{conventions.DocCommentPrefix}@param {conventions.GetParameterDocNullable(x, x)} {x.Description}"
            };
        }
        
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
            var pathParametersParam = codeMethod.Parameters.OfKind(CodeParameterKind.PathParameters);
            var requestAdapterParam = codeMethod.Parameters.FirstOrDefault(x => x.ParameterKind == CodeParameterKind.RequestAdapter);
            if (isConstructor && codeMethod?.Parent is CodeClass @class && @class.IsOfKind(CodeClassKind.RequestBuilder))
            {
                var pathParameters = codeMethod.Parameters
                    .Where(parameter => parameter.IsOfKind(CodeParameterKind.Path))
                    .Select(parameter => $"{conventions.GetParameterSignature(parameter, codeMethod)}");
                var pathParamsString = string.Empty;
                var parameters = pathParameters.ToList();
                if (parameters.Any()) pathParamsString = $", {string.Join(", ", parameters)}";
                var pathParametersString = pathParametersParam != null ? $"{conventions.GetParameterSignature(pathParametersParam, codeMethod)}, " : string.Empty;
                writer.WriteLine($"{conventions.GetAccessModifier(codeMethod.Access)} function {methodName}({pathParametersString}{conventions.GetParameterSignature(requestAdapterParam, codeMethod)}{pathParamsString}) {{");
            }
            else
            {
                writer.WriteLine(
                    $"{conventions.GetAccessModifier(codeMethod.Access)} function {methodPrefix}{methodName}({methodParameters}){(!codeMethod.IsOfKind(CodeMethodKind.RequestExecutor) ? $"{returnValue}" : ": Promise")} {{");
            }

            writer.IncreaseIndent();
            
        }

        private void WriteSerializerBody(CodeMethod codeMethod, CodeClass parentClass, LanguageWriter writer, bool inherits)
        {
            var additionalDataProperty = parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData).FirstOrDefault();
            var writerParameter = codeMethod.Parameters.FirstOrDefault(x => x.ParameterKind == CodeParameterKind.Serializer);
            var writerParameterName = conventions.GetParameterName(writerParameter);
            if(inherits)
                writer.WriteLine($"parent::serialize({writerParameterName});");
            var customProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom);
            foreach(var otherProp in customProperties) {
                writer.WriteLine($"{writerParameterName}->{GetSerializationMethodName(otherProp.Type)}('{otherProp.SerializationName ?? otherProp.Name.ToFirstCharacterLowerCase()}', $this->{otherProp.Name.ToFirstCharacterLowerCase()});");
            }
            if(additionalDataProperty != null)
                writer.WriteLine($"{writerParameterName}->writeAdditionalData($this->{additionalDataProperty.Name.ToFirstCharacterLowerCase()});");
        }
        
        private string GetSerializationMethodName(CodeTypeBase propType) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = conventions.TranslateType(propType);
            if(propType is CodeType currentType) {
                if(isCollection) { 
                    if(currentType.TypeDefinition is null or CodeEnum){
                        return "writeCollectionOfNonParsableObjectValues";
                    }
                    return currentType.TypeDefinition is CodeEnum ? "writeCollectionOfEnumValues" : "writeCollectionOfObjectValues";
                }

                if (currentType.TypeDefinition is CodeEnum)
                {
                    return "writeEnumValue";
                }

                if (currentType.TypeDefinition is CodeClass cc && cc.IsOfKind(CodeClassKind.Model))
                {
                    return "writeObjectValue";
                }
            }

            var lowerCaseProp = propertyType.ToLower();
            return lowerCaseProp switch
            {
                "string" or "guid" => "writeStringValue",
                "enum" => "writeEnumValue",
                "bool" => "writeBooleanValue",
                "float" or "double" => "writeFloatValue",
                "boolean" or "date" or "time" or "datetime" =>
                    $"write{propertyType.ToFirstCharacterUpperCase()}Value",
                "datetimeoffset" => "writeDateTimeValue",
                "duration" or "timespan" or "dateinterval" => "writeDateIntervalValue",
                "int" => "writeIntegerValue",
                _ => "writeAnyValue"
            };
        }
        
        private string GetDeserializationMethodName(CodeTypeBase propType, CodeMethod method, CodeProperty property) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = conventions.GetTypeString(propType, method, false);
            if(propType is CodeType currentType) {
                if(isCollection)
                    if(currentType.TypeDefinition is null or CodeEnum)
                        return $"$n->getCollectionOfPrimitiveValues()";
                    else if (currentType.TypeDefinition is CodeEnum enumType)
                        return $"$n->getCollectionOfEnumValues({enumType.Name.ToFirstCharacterUpperCase()}::class)";
                    else
                        return $"$n->getCollectionOfObjectValues({conventions.TranslateType(propType)}::class)";
                else if (currentType.TypeDefinition is CodeEnum)
                    return $"$n->getEnumValue({propertyType.ToFirstCharacterUpperCase()}::class)";
            }

            var lowerCaseType = propertyType.ToLower();
            return lowerCaseType switch
            {
                "int" => $"$n->getIntegerValue()",
                "bool" => "$n->getBooleanValue()",
                _ when conventions.PrimitiveTypes.Contains(lowerCaseType) => $"$n->get{propertyType.ToFirstCharacterUpperCase()}Value()",
                _ => $"$n->getObjectValue({propertyType.ToFirstCharacterUpperCase()}::class)",
            };
        }

        private static void WriteSetterBody(LanguageWriter writer, CodeMethod codeElement)
        {
            var propertyName = codeElement.AccessedProperty?.Name;
            writer.WriteLine($"$this->{propertyName.ToFirstCharacterLowerCase()} = $value;");
        }

        private static void WriteGetterBody(LanguageWriter writer, CodeMethod codeMethod)
        {
            var propertyName = codeMethod.AccessedProperty?.Name.ToFirstCharacterLowerCase();
            writer.WriteLine($"return $this->{propertyName};");
        }

        private void WriteRequestBuilderWithParametersBody(string returnType, LanguageWriter writer, CodeElement element = default)
        {
            conventions.AddRequestBuilderBody(returnType, writer, default, element);
        }
        
        private static string GetPropertyCall(CodeProperty property, string defaultValue) => property == null ? defaultValue : $"$this->{property.Name}";
        private void WriteRequestGeneratorBody(CodeMethod codeElement, RequestParams requestParams, CodeClass currentClass, LanguageWriter writer) 
        {
            if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
            var requestInformationClass = "RequestInformation";
            var pathParametersProperty = currentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
            var urlTemplateProperty = currentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate);
            var requestAdapterProperty = currentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
            writer.WriteLines($"{RequestInfoVarName} = new {requestInformationClass}();",
                                $"{RequestInfoVarName}->urlTemplate = {GetPropertyCall(urlTemplateProperty, "''")};",
                                $"{RequestInfoVarName}->pathParameters = {GetPropertyCall(pathParametersProperty, "''")};",
                                $"{RequestInfoVarName}->httpMethod = HttpMethod::{codeElement?.HttpMethod?.ToString().ToUpperInvariant()};");
            //TODO: Fix this if(requestParams.headers != null)
            // writer.WriteLine($"{RequestInfoVarName}->setHeadersFromRawObject({conventions.GetParameterName(requestParams.headers)});");
            if (requestParams.queryString != null)
            {
                writer.WriteLine($"if ({conventions.GetParameterName(requestParams.queryString)} !== null) {{");
                writer.IncreaseIndent();
                writer.WriteLines(
                    $"{RequestInfoVarName}->setQueryParameters({conventions.GetParameterName(requestParams.queryString)});");
                writer.DecreaseIndent();
                writer.WriteLine("}");
            }

            if(requestParams.requestBody != null) {
                if(requestParams.requestBody.Type.Name.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
                    writer.WriteLine($"{RequestInfoVarName}->setStreamContent({conventions.GetParameterName(requestParams.requestBody)});");
                else {
                    var spreadOperator = requestParams.requestBody.Type.AllTypes.First().IsCollection ? "..." : string.Empty;
                    writer.WriteLine($"{RequestInfoVarName}->setContentFromParsable($this->{requestAdapterProperty.Name.ToFirstCharacterLowerCase()}, \"{codeElement.ContentType}\", {spreadOperator}{conventions.GetParameterName(requestParams.requestBody)});");
                }
            }

            if (requestParams.options != null)
            {
                writer.WriteLine($"if ({conventions.GetParameterName(requestParams.options)} !== null) {{");
                writer.IncreaseIndent();
                writer.WriteLine($"{RequestInfoVarName}->addRequestOptions(...$options);");
                writer.DecreaseIndent();
                writer.WriteLine("}");
            }

            writer.WriteLine($"return {RequestInfoVarName};");
        }
        private void WriteDeserializerBody(CodeClass parentClass, LanguageWriter writer, CodeMethod method) {
            var inherits = (parentClass.StartBlock as CodeClass.Declaration)?.Inherits != null;
            var fieldToSerialize = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom);
            writer.WriteLine($"return {(inherits ? "array_merge(parent::getFieldDeserializers()," : string.Empty)} [");
            if(fieldToSerialize.Any()) {
                writer.IncreaseIndent();
                fieldToSerialize
                    .OrderBy(x => x.Name)
                    .Select(x => 
                        $"'{x.SerializationName ?? x.Name.ToFirstCharacterLowerCase()}' => function (self $o, ParseNode $n) {{ $o->set{x.Name.ToFirstCharacterUpperCase()}({GetDeserializationMethodName(x.Type, method, x)}); }},")
                    .ToList()
                    .ForEach(x => writer.WriteLine(x));
                writer.DecreaseIndent();
            }
            writer.WriteLine($"]{(inherits ? ')': string.Empty )};");
        }
        
        private void WriteIndexerBody(CodeMethod codeElement, CodeClass parentClass, string returnType, LanguageWriter writer) {
            var pathParametersProperty = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
            conventions.AddParametersAssignment(writer, pathParametersProperty.Type, $"$this->{pathParametersProperty.Name}",
                (codeElement.OriginalIndexer.IndexType, codeElement.OriginalIndexer.ParameterName, "$id"));
            conventions.AddRequestBuilderBody(parentClass, returnType, writer, conventions.TempDictionaryVarName);
        }

        private void WriteRequestExecutorBody(CodeMethod codeElement, CodeClass parentClass, RequestParams requestParams, LanguageWriter writer)
        {
            var generatorMethod = (codeElement.Parent as CodeClass)?
                .Methods
                .FirstOrDefault(x =>
                    x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod);
            var generatorMethodName = generatorMethod?.Name.ToFirstCharacterLowerCase();
            var requestInfoParameters = new CodeParameter[] { requestParams.requestBody, requestParams.queryString, requestParams.headers, requestParams.options }
                .Select(x => x).Where(x => x?.Name != null);
            var infoParameters = requestInfoParameters as CodeParameter[] ?? requestInfoParameters.ToArray();
            var callParams = infoParameters.Select(x => conventions.GetParameterName(x));
            var joinedParams = string.Empty; 
            var requestAdapterProperty = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
            if(infoParameters.Any())
            {
                joinedParams = string.Join(", ", callParams);
            }
            
            var returnType = conventions.TranslateType(codeElement.ReturnType);
            var returnVoidOrString = returnType.Equals("void", StringComparison.OrdinalIgnoreCase) || returnType.Equals("string", StringComparison.OrdinalIgnoreCase);
            writer.WriteLine($"$requestInfo = $this->{generatorMethodName}({joinedParams});");
            writer.WriteLine("try {");
            writer.IncreaseIndent();
            if(codeElement.Parameters.Any(x => x.IsOfKind(CodeParameterKind.ResponseHandler)))
                writer.WriteLine($"return {GetPropertyCall(requestAdapterProperty, string.Empty)}->sendAsync({RequestInfoVarName}, {(!returnVoidOrString ? $"{returnType}::class": "''")}, $responseHandler);");
            else
                writer.WriteLine($"return {GetPropertyCall(requestAdapterProperty, string.Empty)}->sendAsync({RequestInfoVarName}, {(!returnVoidOrString ? $"{returnType}::class": "''")}, null);");
            writer.DecreaseIndent();
            writer.WriteLine("} catch(Exception $ex) {");
            writer.IncreaseIndent();
            writer.WriteLine("return new RejectedPromise($ex);");
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }

        private static void WriteApiConstructorBody(CodeClass parentClass, CodeMethod codeMethod, LanguageWriter writer)
        {
            var requestAdapterProperty = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
            WriteSerializationRegistration(codeMethod.SerializerModules, writer, "registerDefaultSerializer");
            WriteSerializationRegistration(codeMethod.DeserializerModules, writer, "registerDefaultDeserializer");
            writer.WriteLine($"{GetPropertyCall(requestAdapterProperty, string.Empty)}->setBaseUrl('{codeMethod.BaseUrl}');");
        }
        
        private static void WriteSerializationRegistration(List<string> serializationModules, LanguageWriter writer, string methodName) {
            if(serializationModules != null)
                foreach(var module in serializationModules)
                    writer.WriteLine($"ApiClientBuilder::{methodName}(\\{module.ReplaceDotsWithSlashInNamespaces()}::class);");
        }
    }
}
