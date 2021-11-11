using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
            var currentPathProperty = parentClass?.Properties.FirstOrDefault(x => x.IsOfKind(CodePropertyKind.PathParameters));
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
                        WriteDeserializerBody(codeElement, codeElement, parentClass, writer);
                        break;
                    case CodeMethodKind.RequestBuilderWithParameters:
                        WriteRequestBuilderWithParametersBody(codeElement, currentPathProperty, returnType, writer);
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
            conventions.WriteCodeBlockEnd(writer);
            writer.WriteLine();
        }
        
        private void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer, bool inherits) {
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
                writer.WriteLine($"$this->{propWithDefault.NamePrefix}{propWithDefault.Name.ToFirstCharacterLowerCase()} = {(isPathSegment ? "[]" :conventions.ReplaceDoubleQuoteWithSingleQuote(propWithDefault.DefaultValue))};");
            }
            if(currentMethod.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor)) {
                AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.RequestAdapter, CodePropertyKind.RequestAdapter, writer);
                AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.PathParameters, CodePropertyKind.PathParameters, writer);
                AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.RawUrl, CodePropertyKind.UrlTemplate, writer);
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
                        _ => $"{conventions.DocCommentPrefix}@param {conventions.GetParameterDocNullable(x, x)} {x.Description}"
                    };
                })
                .ToList()
                .ForEach(x => writer.WriteLine(x));
            var returnDocString = codeMethod.MethodKind switch
                {
                    CodeMethodKind.Deserializer => "array<string, callable>",
                    CodeMethodKind.Getter when accessedProperty.Type.IsArray || accessedProperty.Type.IsCollection => $"array<{conventions.TranslateType(accessedProperty.Type)}>",
                    CodeMethodKind.Getter => isGetterForAdditionalData
                        ? "array<string, mixed>"
                        : conventions.GetTypeString(codeMethod.ReturnType, codeMethod),
                    _ => conventions.GetTypeString(codeMethod.ReturnType, codeMethod)
                };
            var isRequestExecutor = codeMethod.MethodKind == CodeMethodKind.RequestExecutor;
            if (!isVoidable || isRequestExecutor)
            {
                writer.WriteLines(
                    $"{conventions.DocCommentPrefix}@return {(isRequestExecutor ? "Promise" : $"{returnDocString}{orNullReturn[1]}")}"
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
            var isRequestExecutor = codeMethod.MethodKind == CodeMethodKind.RequestExecutor;
            writer.WriteLine($"{conventions.GetAccessModifier(codeMethod.Access)} function {methodPrefix}{methodName}({methodParameters}){(isRequestExecutor ? ": Promise": returnValue)} {{");
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

        private void WriteRequestBuilderWithParametersBody(CodeMethod codeElement, CodeProperty currentPathProperty, string returnType, LanguageWriter writer)
        {
            var codePathParameters = codeElement.Parameters
                .Where(x => x.IsOfKind(CodeParameterKind.Path))
                .Select(x => x.Name);
            var codePathParametersSuffix = codePathParameters.Any() ? 
                ", " + codePathParameters.Aggregate((x, y) => $"{x}, {y}") :
                string.Empty;
            conventions.AddRequestBuilderBody(currentPathProperty != null, returnType, writer, additionalPathParameters: codePathParametersSuffix);
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
            if(requestParams.headers != null)
                writer.WriteLine($"{RequestInfoVarName}->setHeadersFromRawObject({conventions.GetParameterName(requestParams.headers)});");
            if(requestParams.queryString != null)
                writer.WriteLines($"{RequestInfoVarName}->setQueryStringParametersFromRawObject({conventions.GetParameterName(requestParams.queryString)});");
            if(requestParams.requestBody != null) {
                if(requestParams.requestBody.Type.Name.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
                    writer.WriteLine($"{RequestInfoVarName}->setStreamContent({conventions.GetParameterName(requestParams.requestBody)});");
                else {
                    var spreadOperator = requestParams.requestBody.Type.AllTypes.First().IsCollection ? "..." : string.Empty;
                    writer.WriteLine($"{RequestInfoVarName}->setContentFromParsable($this->{requestAdapterProperty.Name.ToFirstCharacterLowerCase()}, \"{codeElement.ContentType}\", {spreadOperator}{conventions.GetParameterName(requestParams.requestBody)});");
                }
            }
            if(requestParams.options != null)
                writer.WriteLine($"{RequestInfoVarName}->addRequestOptions(...$options);");
            writer.WriteLine($"return {RequestInfoVarName};");
        }
        private void WriteDeserializerBody(CodeMethod codeElement, CodeMethod method, CodeClass parentClass, LanguageWriter writer) {
            var inherits = (parentClass.StartBlock as CodeClass.Declaration).Inherits != null;
            var fieldToSerialize = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom);
            writer.WriteLine($"return {(inherits ? "array_merge(parent::getFieldDeserializers()," : string.Empty)} [");
            if(fieldToSerialize.Any()) {
                writer.IncreaseIndent();
                fieldToSerialize
                    .OrderBy(x => x.Name)
                    .Select(x => 
                        $"'{x.SerializationName ?? x.Name.ToFirstCharacterLowerCase()}' => function ({parentClass.Name.ToFirstCharacterUpperCase()} $o, {conventions.GetTypeString(x.Type, x)} $n) {{ $o->set{x.Name.ToFirstCharacterUpperCase()}($n); }},")
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
            writer.WriteLine($"$requestInfo = $this->{generatorMethodName}({joinedParams});");
            writer.WriteLine($"return {GetPropertyCall(requestAdapterProperty, string.Empty)}->sendAsync({RequestInfoVarName}, get_class($body), $responseHandler);");
        }

        private static void WriteApiConstructorBody(CodeClass parentClass, CodeMethod codeMethod, LanguageWriter writer)
        {
            var requestAdapterProperty = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
            writer.WriteLine($"{GetPropertyCall(requestAdapterProperty, string.Empty)}->setBaseUrl('{codeMethod.BaseUrl}');");
        }
    }
}
