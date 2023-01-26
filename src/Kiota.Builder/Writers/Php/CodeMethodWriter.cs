using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
namespace Kiota.Builder.Writers.Php
{
    public class CodeMethodWriter: BaseElementWriter<CodeMethod, PhpConventionService>
    {

        protected readonly bool UseBackingStore = false;
        public CodeMethodWriter(PhpConventionService conventionService, bool useBackingStore = false) : base(conventionService)
        {
            UseBackingStore = useBackingStore;
        }
        
        private const string RequestInfoVarName = "$requestInfo";
        private const string CreateDiscriminatorMethodName = "createFromDiscriminatorValue";
        public override void  WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
        {

            var parentClass = codeElement.Parent as CodeClass;
            var returnType = codeElement.Kind == CodeMethodKind.Constructor ? "void" : conventions.GetTypeString(codeElement.ReturnType, codeElement);
            var inherits = parentClass?.StartBlock?.Inherits != null;
            var extendsModelClass = inherits && parentClass?.StartBlock?.Inherits?.TypeDefinition is CodeClass codeClass &&
                                     codeClass.IsOfKind(CodeClassKind.Model);
            var orNullReturn = codeElement.ReturnType.IsNullable ? new[]{"?", "|null"} : new[] {string.Empty, string.Empty};
            var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
            var config = codeElement.Parameters.OfKind(CodeParameterKind.RequestConfiguration);
            var requestParams = new RequestParams(requestBodyParam, config);
            
            WriteMethodPhpDocs(codeElement, writer, orNullReturn);
            WriteMethodsAndParameters(codeElement, writer, orNullReturn, codeElement.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor));

            switch (codeElement.Kind)
            {
                    case CodeMethodKind.Constructor: 
                        WriteConstructorBody(parentClass, codeElement, writer, inherits);
                        break;
                    case CodeMethodKind.Serializer:
                        WriteSerializerBody(parentClass, writer, extendsModelClass);
                        break;
                    case CodeMethodKind.Setter:
                        WriteSetterBody(writer, codeElement);
                        break;
                    case CodeMethodKind.Getter:
                        WriteGetterBody(writer, codeElement);
                        break;
                    case CodeMethodKind.Deserializer:
                        WriteDeserializerBody(parentClass, writer, codeElement, extendsModelClass);
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
                    case CodeMethodKind.Factory:
                        WriteFactoryMethodBody(codeElement, parentClass, writer);
                        break;
            }
            writer.CloseBlock();
            writer.WriteLine();
        }
        
        private static void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer, bool inherits) {
            if(inherits)
                writer.WriteLine("parent::__construct();");
            var backingStoreProperty = parentClass.GetPropertyOfKind(CodePropertyKind.BackingStore);
            if (backingStoreProperty != null && !string.IsNullOrEmpty(backingStoreProperty.DefaultValue))
                writer.WriteLine($"$this->{backingStoreProperty.Name.ToFirstCharacterLowerCase()} = {backingStoreProperty.DefaultValue};");
            foreach(var propWithDefault in parentClass.GetPropertiesOfKind(
                    CodePropertyKind.RequestBuilder,
                    CodePropertyKind.UrlTemplate,
                    CodePropertyKind.PathParameters)
                .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
                .OrderByDescending(x => x.Kind)
                .ThenBy(x => x.Name))
            {
                var isPathSegment = propWithDefault.IsOfKind(CodePropertyKind.PathParameters);
                writer.WriteLine($"$this->{propWithDefault.Name.ToFirstCharacterLowerCase()} = {(isPathSegment ? "[]" :propWithDefault.DefaultValue.ReplaceDoubleQuoteWithSingleQuote())};");
            }
            foreach(var propWithDefault in parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData, CodePropertyKind.Custom) //additional data and custom properties rely on accessors
                .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
                // do not apply the default value if the type is composed as the default value may not necessarily which type to use
                .Where(static x => x.Type is not CodeType propType || propType.TypeDefinition is not CodeClass propertyClass || propertyClass.OriginalComposedType is null)
                .OrderBy(x => x.Name)) {
                var setterName = propWithDefault.SetterFromCurrentOrBaseType?.Name.ToFirstCharacterLowerCase() ?? $"set{propWithDefault.SymbolName.ToFirstCharacterUpperCase()}";
                writer.WriteLine($"$this->{setterName}({propWithDefault.DefaultValue.ReplaceDoubleQuoteWithSingleQuote()});");
            }
            if(currentMethod.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor)) {
                AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.RequestAdapter, CodePropertyKind.RequestAdapter, writer);
                AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.PathParameters, CodePropertyKind.PathParameters, writer);
                AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.RawUrl, CodePropertyKind.UrlTemplate, writer);
            }
            var pathParametersProperty = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
            var pathParametersParameter = currentMethod.Parameters.OfKind(CodeParameterKind.PathParameters);
            var urlTemplateTempVarName = "$urlTplParams";
            if (currentMethod.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor) &&
                parentClass.IsOfKind(CodeClassKind.RequestBuilder) &&
                currentMethod.Parameters.Any(x => x.IsOfKind(CodeParameterKind.Path)))
            {
                writer.WriteLine($"{urlTemplateTempVarName} = ${pathParametersParameter.Name};");
                currentMethod.Parameters.Where(parameter => parameter.IsOfKind(CodeParameterKind.Path)).ToList()
                    .ForEach(parameter =>
                    {
                        writer.WriteLine($"{urlTemplateTempVarName}['{parameter.Name}'] = ${parameter.Name.ToFirstCharacterLowerCase()};");
                    });
                writer.WriteLine($"{GetPropertyCall(pathParametersProperty, "[]")} = array_merge({GetPropertyCall(pathParametersProperty, "[]")}, {urlTemplateTempVarName});");
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
            var methodDescription = codeMethod.Documentation.Description ?? string.Empty;

            var hasMethodDescription = !string.IsNullOrEmpty(methodDescription.Trim(' '));
            var parameters = codeMethod.Parameters;
            var withDescription = parameters as CodeParameter[] ?? parameters.ToArray();
            if (!hasMethodDescription && !withDescription.Any())
            {
                return;
            }
            var isVoidable = "void".Equals(conventions.GetTypeString(codeMethod.ReturnType, codeMethod),
                StringComparison.OrdinalIgnoreCase) && !codeMethod.IsOfKind(CodeMethodKind.RequestExecutor);

            var accessedProperty = codeMethod.AccessedProperty;
            var isSetterForAdditionalData = (codeMethod.IsOfKind(CodeMethodKind.Setter) &&
                                             accessedProperty.IsOfKind(CodePropertyKind.AdditionalData));

            var parametersWithDescription = withDescription
                .Where(x => x.Documentation.DescriptionAvailable)
                .Select(x => GetParameterDocString(codeMethod, x, isSetterForAdditionalData))
                .ToList();
            var returnDocString = GetDocCommentReturnType(codeMethod, accessedProperty);
            if (!isVoidable)
                returnDocString = (codeMethod.Kind == CodeMethodKind.RequestExecutor)
                    ? "@return Promise"
                    : $"@return {returnDocString}{orNullReturn[1]}";
            else returnDocString = String.Empty;
            conventions.WriteLongDescription(codeMethod.Documentation,
                writer,
                parametersWithDescription.Union(new []{returnDocString})
                );

        }

        private string GetDocCommentReturnType(CodeMethod codeMethod, CodeProperty accessedProperty)
        {
            return codeMethod.Kind switch
            {
                CodeMethodKind.Deserializer => "array<string, callable>",
                CodeMethodKind.Getter when accessedProperty.IsOfKind(CodePropertyKind.AdditionalData) => "array<string, mixed>",
                CodeMethodKind.Getter when accessedProperty.Type.IsArray || accessedProperty.Type.IsCollection => $"array<{conventions.TranslateType(accessedProperty.Type)}>",
                _ => conventions.GetTypeString(codeMethod.ReturnType, codeMethod)
            };
        }

        private string GetParameterDocString(CodeMethod codeMethod, CodeParameter x, bool isSetterForAdditionalData = false)
        {
            return codeMethod.Kind switch
            {
                CodeMethodKind.Setter => $"@param {(isSetterForAdditionalData ? "array<string,mixed> $value": conventions.GetParameterDocNullable(x, x))} {x?.Documentation.Description}",
                _ => $"@param {conventions.GetParameterDocNullable(x, x)} {x.Documentation.Description}"
            };
        }
        
        private static readonly BaseCodeParameterOrderComparer parameterOrderComparer = new();
        private void WriteMethodsAndParameters(CodeMethod codeMethod, LanguageWriter writer, IReadOnlyList<string> orNullReturn, bool isConstructor = false)
        {
            var methodParameters = string.Join(", ", codeMethod.Parameters
                                                                .Order(parameterOrderComparer)
                                                                .Select(x => conventions.GetParameterSignature(x, codeMethod))
                                                                .ToList());

            var methodName = codeMethod.Kind switch
            {
                CodeMethodKind.Constructor or CodeMethodKind.ClientConstructor => "__construct",
                _ => codeMethod.Name.ToFirstCharacterLowerCase()
            };
            if(codeMethod.IsOfKind(CodeMethodKind.Deserializer))
            {
                writer.WriteLine($"{conventions.GetAccessModifier(codeMethod.Access)}{(codeMethod.IsStatic ? " static" : string.Empty)} function getFieldDeserializers(): array {{");
                writer.IncreaseIndent();
                return;
            }

            if (codeMethod.IsOfKind(CodeMethodKind.Getter) && codeMethod.AccessedProperty.IsOfKind(CodePropertyKind.AdditionalData))
            {
                writer.WriteLine($"{conventions.GetAccessModifier(codeMethod.Access)} function {methodName}(): ?array {{");
                writer.IncreaseIndent();
                return;
            }
            var isVoidable = "void".Equals(isConstructor ? null : conventions.GetTypeString(codeMethod.ReturnType, codeMethod), StringComparison.OrdinalIgnoreCase);
            var optionalCharacterReturn = isVoidable ? string.Empty : orNullReturn[0];
            var returnValue = isConstructor ? string.Empty : $": {optionalCharacterReturn}{conventions.GetTypeString(codeMethod.ReturnType, codeMethod)}";
            if (isConstructor && codeMethod?.Parent is CodeClass @class && @class.IsOfKind(CodeClassKind.RequestBuilder))
            {
                writer.WriteLine($"{conventions.GetAccessModifier(codeMethod.Access)}{(codeMethod.IsStatic ? " static" : string.Empty)} function {methodName}({methodParameters}) {{");
            }
            else
            {
                writer.WriteLine(
                    $"{conventions.GetAccessModifier(codeMethod.Access)} {(codeMethod.IsStatic ? "static " : string.Empty)}function {methodName}({methodParameters}){(!codeMethod.IsOfKind(CodeMethodKind.RequestExecutor) ? $"{returnValue}" : ": Promise")} {{");
            }

            writer.IncreaseIndent();
        }

        private void WriteSerializerBody(CodeClass parentClass, LanguageWriter writer, bool extendsModelClass = false)
        {
            if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType)
                WriteSerializerBodyForUnionModel(parentClass, writer);
            else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
                WriteSerializerBodyForIntersectionModel(parentClass, writer);
            else
                WriteSerializerBodyForInheritedModel(parentClass, writer, extendsModelClass);
        
            var additionalDataProperty = parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData).FirstOrDefault();
        
            if(additionalDataProperty != null)
                writer.WriteLine($"$writer->writeAdditionalData($this->{additionalDataProperty.Getter.Name}());");
        }

        private void WriteSerializerBodyForIntersectionModel(CodeClass parentClass, LanguageWriter writer)
        { 
            var includeElse = false;
            var otherProps = parentClass
                                    .GetPropertiesOfKind(CodePropertyKind.Custom)
                                    .Where(static x => !x.ExistsInBaseType)
                                    .Where(static x => x.Type is not CodeType propertyType || propertyType.IsCollection || propertyType.TypeDefinition is not CodeClass)
                                    .Order(CodePropertyTypeBackwardComparer)
                                    .ThenBy(static x => x.Name)
                                    .ToArray();
            foreach (var otherProp in otherProps)
            {
                writer.StartBlock($"{(includeElse? "} else " : string.Empty)}if ($this->{otherProp.Getter.Name.ToFirstCharacterLowerCase()}() !== null) {{");
                WriteSerializationMethodCall(otherProp, writer, "null");
                writer.DecreaseIndent();
                if(!includeElse)
                    includeElse = true;
            }
            var complexProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                                .Where(static x => x.Type is CodeType { TypeDefinition: CodeClass } && !x.Type.IsCollection)
                                                .ToArray();
            if(complexProperties.Any()) {
                if(includeElse) {
                    writer.WriteLine("} else {");
                    writer.IncreaseIndent();
                }
                var propertiesNames = complexProperties
                                    .Select(static x => $"$this->{x.Getter.Name.ToFirstCharacterLowerCase()}()")
                                    .Order(StringComparer.OrdinalIgnoreCase)
                                    .Aggregate(static (x, y) => $"{x}, {y}");
                WriteSerializationMethodCall(complexProperties.First(), writer, "null", propertiesNames);
                if(includeElse) {
                    writer.CloseBlock();
                }
            } else if(otherProps.Any()) {
                writer.CloseBlock(decreaseIndent: false);
            }
        }

        private void WriteSerializerBodyForUnionModel(CodeClass parentClass, LanguageWriter writer)
        {
            var includeElse = false;
            var otherProps = parentClass
                .GetPropertiesOfKind(CodePropertyKind.Custom)
                .Where(static x => !x.ExistsInBaseType)
                .Order(CodePropertyTypeForwardComparer)
                .ThenBy(static x => x.Name)
                .ToArray();
            foreach (var otherProp in otherProps)
            {
                writer.StartBlock($"{(includeElse? "} else " : string.Empty)}if ($this->{otherProp.Getter.Name.ToFirstCharacterLowerCase()}() !== null) {{");
                WriteSerializationMethodCall(otherProp, writer, "null");
                writer.DecreaseIndent();
                if(!includeElse)
                    includeElse = true;
            }
            if(otherProps.Any())
                writer.CloseBlock(decreaseIndent: false);
        }
        
        private void WriteSerializationMethodCall(CodeProperty otherProp, LanguageWriter writer, string serializationKey, string dataToSerialize = default) {
            if(string.IsNullOrEmpty(dataToSerialize))
                dataToSerialize = $"$this->{otherProp.Getter?.Name?.ToFirstCharacterLowerCase() ?? "get" + otherProp.Name.ToFirstCharacterUpperCase()}()";
            writer.WriteLine($"$writer->{GetSerializationMethodName(otherProp.Type)}({serializationKey}, {dataToSerialize});");
        }
        
        private void WriteSerializerBodyForInheritedModel(CodeClass parentClass, LanguageWriter writer, bool extendsModelClass = false)
        {
            if(extendsModelClass)
                writer.WriteLine("parent::serialize($writer);");
            foreach(var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom).Where(static x => !x.ExistsInBaseType && !x.ReadOnly))
                WriteSerializationMethodCall(otherProp, writer, $"'{otherProp.SerializationName ?? otherProp.Name.ToFirstCharacterLowerCase()}'");
        }

        private string GetSerializationMethodName(CodeTypeBase propType) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = conventions.TranslateType(propType);
            if(propType is CodeType currentType) {
                if(isCollection) { 
                    if(currentType.TypeDefinition is null){
                        return "writeCollectionOfPrimitiveValues";
                    }
                    return currentType.TypeDefinition is CodeEnum ? "writeCollectionOfEnumValues" : "writeCollectionOfObjectValues";
                }

                if (currentType.TypeDefinition is CodeEnum)
                {
                    return "writeEnumValue";
                }
            }

            var lowerCaseProp = propertyType?.ToLower();
            return lowerCaseProp switch
            {
                "string" or "guid" => "writeStringValue",
                "enum" or "float" or "date" or "time" or "byte" => $"write{lowerCaseProp.ToFirstCharacterUpperCase()}Value",
                "bool" or "boolean" => "writeBooleanValue",
                "double" or "decimal" => "writeFloatValue",
                "datetime" or "datetimeoffset" => "writeDateTimeValue",
                "duration" or "timespan" or "dateinterval" => "writeDateIntervalValue",
                "int" or "number" => "writeIntegerValue",
                "streaminterface" => "writeBinaryContent",
                _ when conventions.PrimitiveTypes.Contains(lowerCaseProp) => $"write{lowerCaseProp.ToFirstCharacterUpperCase()}Value",
                _ => "writeObjectValue"
            };
        }

        private const string ParseNodeVarName = "$parseNode";
        private string GetDeserializationMethodName(CodeTypeBase propType, CodeMethod method) {
            var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
            var propertyType = conventions.GetTypeString(propType, method, false);
            var parseNodeMethod = string.Empty;
            if(propType is CodeType currentType)
            {
                if(isCollection)
                    parseNodeMethod = currentType.TypeDefinition switch
                    {
                        null => "getCollectionOfPrimitiveValues()",
                        CodeEnum enumType => $"getCollectionOfEnumValues({enumType.Name.ToFirstCharacterUpperCase()}::class)",
                        _ => $"getCollectionOfObjectValues([{conventions.TranslateType(propType)}::class, '{CreateDiscriminatorMethodName}'])"
                    };
                else if (currentType.TypeDefinition is CodeEnum)
                    parseNodeMethod =  $"getEnumValue({propertyType.ToFirstCharacterUpperCase()}::class)";
            }

            var lowerCaseType = propertyType?.ToLower();
            parseNodeMethod =  string.IsNullOrEmpty(parseNodeMethod) ? lowerCaseType switch
            {
                "int" => "getIntegerValue()",
                "bool" => "getBooleanValue()",
                "number" => "getIntegerValue()",
                "decimal" or "double" => "getFloatValue()",
                "streaminterface" => "getBinaryContent()",
                "byte" => "getByteValue()",
                _ when conventions.PrimitiveTypes.Contains(lowerCaseType) => $"get{propertyType.ToFirstCharacterUpperCase()}Value()",
                _ => $"getObjectValue([{propertyType.ToFirstCharacterUpperCase()}::class, '{CreateDiscriminatorMethodName}'])",
            } : parseNodeMethod;
            return parseNodeMethod;
        }

        private void WriteSetterBody(LanguageWriter writer, CodeMethod codeElement)
        {
            var propertyName = codeElement.AccessedProperty?.Name.ToFirstCharacterLowerCase();
            var parentClass = codeElement.Parent as CodeClass;
            var isBackingStoreSetter = codeElement.AccessedProperty?.Kind == CodePropertyKind.BackingStore;
            if (UseBackingStore && !isBackingStoreSetter)
                writer.WriteLine($"$this->{parentClass.GetBackingStoreProperty()?.Getter.Name}()->set('{propertyName.ToFirstCharacterLowerCase()}', $value);");
            else
                writer.WriteLine($"$this->{propertyName.ToFirstCharacterLowerCase()} = $value;");
        }

        private void WriteGetterBody(LanguageWriter writer, CodeMethod codeMethod)
        {
            var propertyName = codeMethod.AccessedProperty?.Name.ToFirstCharacterLowerCase();
            var parentClass = codeMethod.Parent as CodeClass;
            var isBackingStoreGetter = codeMethod.AccessedProperty?.Kind == CodePropertyKind.BackingStore;
            if (UseBackingStore && !isBackingStoreGetter)
                writer.WriteLine($"return $this->{parentClass.GetBackingStoreProperty()?.Getter.Name}()->get('{propertyName}');");
            else
                writer.WriteLine($"return $this->{propertyName};");
        }

        private void WriteRequestBuilderWithParametersBody(string returnType, LanguageWriter writer, CodeElement element = default)
        {
            conventions.AddRequestBuilderBody(returnType, writer, default, element);
        }
        
        private static string GetPropertyCall(CodeProperty property, string defaultValue) => property == null ? defaultValue : $"$this->{property.Name}";
        private void WriteRequestGeneratorBody(CodeMethod codeElement, RequestParams requestParams, CodeClass currentClass, LanguageWriter writer) 
        {
            if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
            var requestInformationClass = "RequestInformation";
            var pathParametersProperty = currentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
            var urlTemplateProperty = currentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate);
            var requestAdapterProperty = currentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
            writer.WriteLines($"{RequestInfoVarName} = new {requestInformationClass}();",
                                $"{RequestInfoVarName}->urlTemplate = {GetPropertyCall(urlTemplateProperty, "''")};",
                                $"{RequestInfoVarName}->pathParameters = {GetPropertyCall(pathParametersProperty, "''")};",
                                $"{RequestInfoVarName}->httpMethod = HttpMethod::{codeElement.HttpMethod?.ToString().ToUpperInvariant()};");
            WriteAcceptHeaderDef(codeElement, writer);
            WriteRequestConfiguration(requestParams, writer);
            if (requestParams.requestBody != null) {
                var suffix = requestParams.requestBody.Type.IsCollection ? "Collection" : string.Empty;
                if (requestParams.requestBody.Type.Name.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
                    writer.WriteLine($"{RequestInfoVarName}->setStreamContent({conventions.GetParameterName(requestParams.requestBody)});");
                else if (requestParams.requestBody.Type is CodeType bodyType && bodyType.TypeDefinition is CodeClass) {
                    writer.WriteLine($"{RequestInfoVarName}->setContentFromParsable{suffix}($this->{requestAdapterProperty.Name.ToFirstCharacterLowerCase()}, \"{codeElement.RequestBodyContentType}\", {conventions.GetParameterName(requestParams.requestBody)});");
                } else {
                    writer.WriteLine($"{RequestInfoVarName}->setContentFromScalar{suffix}($this->{requestAdapterProperty.Name.ToFirstCharacterLowerCase()}, \"{codeElement.RequestBodyContentType}\", {conventions.GetParameterName(requestParams.requestBody)});");
                }
            }

            writer.WriteLine($"return {RequestInfoVarName};");
        }

        private void WriteRequestConfiguration(RequestParams requestParams, LanguageWriter writer)
        {
            if (requestParams.requestConfiguration != null)
            {
                var queryString = requestParams.QueryParameters;
                var headers = requestParams.Headers;
                var options = requestParams.Options;
                var requestConfigParamName = conventions.GetParameterName(requestParams.requestConfiguration);
                writer.WriteLine($"if ({requestConfigParamName} !== null) {{");
                writer.IncreaseIndent();
                if (headers != null) {
                    var headersName = $"{requestConfigParamName}->{headers.Name.ToFirstCharacterLowerCase()}";
                    writer.WriteLine($"if ({headersName} !== null) {{");
                    writer.IncreaseIndent();
                    writer.WriteLine($"{RequestInfoVarName}->addHeaders({headersName});");
                    writer.CloseBlock();
                }
                if (queryString != null)
                {
                    var queryStringName = $"{requestConfigParamName}->{queryString.Name.ToFirstCharacterLowerCase()}";
                    writer.WriteLine($"if ({queryStringName} !== null) {{");
                    writer.IncreaseIndent();
                    writer.WriteLine($"{RequestInfoVarName}->setQueryParameters({queryStringName});");
                    writer.CloseBlock();
                }
                if (options != null)
                {
                    var optionsName = $"{requestConfigParamName}->{options.Name.ToFirstCharacterLowerCase()}";
                    writer.WriteLine($"if ({optionsName} !== null) {{");
                    writer.IncreaseIndent();
                    writer.WriteLine($"{RequestInfoVarName}->addRequestOptions(...{optionsName});");
                    writer.CloseBlock();
                }
                writer.CloseBlock();
            }
        }

        private void WriteAcceptHeaderDef(CodeMethod codeMethod, LanguageWriter writer)
        {
            if(codeMethod.AcceptedResponseTypes.Any())
                writer.WriteLine($"{RequestInfoVarName}->addHeader('Accept', \"{string.Join(", ", codeMethod.AcceptedResponseTypes)}\");");
        }
        private void WriteDeserializerBody(CodeClass parentClass, LanguageWriter writer, CodeMethod method, bool extendsModelClass = false) {
            var inherits = parentClass.StartBlock?.Inherits != null;
            if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType)
                WriteDeserializerBodyForUnionModel(method, parentClass, writer);
            else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
                WriteDeserializerBodyForIntersectionModel(parentClass, writer);
            else
                WriteDeserializerBodyForInheritedModel(method, parentClass, writer, extendsModelClass);
        }
        private void WriteDeserializerBodyForInheritedModel(CodeMethod method, CodeClass parentClass, LanguageWriter writer, bool extendsModelClass = false)
        {
            var fieldToSerialize = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom);
            var codeProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom).ToArray();
            writer.WriteLine("$o = $this;");
            writer.WriteLines(
                $"return {((extendsModelClass) ? $"array_merge(parent::{method.Name.ToFirstCharacterLowerCase()}(), [" : " [" )}");
            writer.IncreaseIndent();
            if(codeProperties.Any()) {
                codeProperties
                    .Where(static x => !x.ExistsInBaseType && x.Setter != null)
                    .OrderBy(static x => x.Name)
                    .Select(x => 
                        $"'{x.SerializationName ?? x.Name.ToFirstCharacterLowerCase()}' => fn(ParseNode $n) => $o->{x.Setter.Name.ToFirstCharacterLowerCase()}($n->{GetDeserializationMethodName(x.Type, method)}),")
                    .ToList()
                    .ForEach(x => writer.WriteLine(x));
            }
            writer.DecreaseIndent();
            writer.WriteLine(extendsModelClass ? "]);" : "];");
        }

        private static void WriteDeserializerBodyForIntersectionModel(CodeClass parentClass, LanguageWriter writer)
        {
            var complexProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                .Where(static x => x.Type is CodeType propType && propType.TypeDefinition is CodeClass && !x.Type.IsCollection)
                .ToArray();
            if(complexProperties.Any()) {
                var propertiesNames = complexProperties
                    .Select(static x => x.Getter.Name.ToFirstCharacterLowerCase())
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var propertiesNamesAsConditions = propertiesNames
                    .Select(static x => $"$this->{x}() !== null")
                    .Aggregate(static (x, y) => $"{x} || {y}");
                writer.StartBlock($"if ({propertiesNamesAsConditions}) {{");
                var propertiesNamesAsArgument = propertiesNames
                    .Select(static x => $"$this->{x}()")
                    .Aggregate(static (x, y) => $"{x}, {y}");
                writer.WriteLine($"return ParseNodeHelper::mergeDeserializersForIntersectionWrapper({propertiesNamesAsArgument});");
                writer.CloseBlock();
            }
            writer.WriteLine($"return [];");
        }

        private static void WriteDeserializerBodyForUnionModel(CodeMethod method, CodeClass parentClass, LanguageWriter writer)
        {
            var includeElse = false;
            var otherPropGetters = parentClass
                .GetPropertiesOfKind(CodePropertyKind.Custom)
                .Where(static x => !x.ExistsInBaseType)
                .Where(static x => x.Type is CodeType propertyType && !propertyType.IsCollection && propertyType.TypeDefinition is CodeClass)
                .Order(CodePropertyTypeForwardComparer)
                .ThenBy(static x => x.Name)
                .Select(static x => x.Getter.Name.ToFirstCharacterLowerCase())
                .ToArray();
            foreach (var otherPropGetter in otherPropGetters)
            {
                writer.StartBlock($"{(includeElse? "} else " : string.Empty)}if ($this->{otherPropGetter}() !== null) {{");
                writer.WriteLine($"return $this->{otherPropGetter}()->{method.Name.ToFirstCharacterLowerCase()}();");
                writer.DecreaseIndent();
                if(!includeElse)
                    includeElse = true;
            }
            if(otherPropGetters.Any())
                writer.CloseBlock(decreaseIndent: false);
            writer.WriteLine($"return [];");
        }

        private void WriteIndexerBody(CodeMethod codeElement, CodeClass parentClass, string returnType, LanguageWriter writer) {
            var pathParametersProperty = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
            conventions.AddParametersAssignment(writer, pathParametersProperty.Type, $"$this->{pathParametersProperty.Name}",
                (codeElement.OriginalIndexer.IndexType, codeElement.OriginalIndexer.SerializationName, "$id"));
            conventions.AddRequestBuilderBody(parentClass, returnType, writer, conventions.TempDictionaryVarName);
        }

        private void WriteRequestExecutorBody(CodeMethod codeElement, CodeClass parentClass, RequestParams requestParams, LanguageWriter writer)
        {
            var generatorMethod = (codeElement.Parent as CodeClass)?
                .Methods
                .FirstOrDefault(x =>
                    x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod);
            var generatorMethodName = generatorMethod?.Name.ToFirstCharacterLowerCase();
            var requestInfoParameters = new[] { requestParams.requestBody, requestParams.requestConfiguration }
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
            writer.WriteLine($"$requestInfo = $this->{generatorMethodName}({joinedParams});");
            writer.WriteLine("try {");
            writer.IncreaseIndent();
            var errorMappings = codeElement.ErrorMappings;
            var hasErrorMappings = false;
            var errorMappingsVarName = "$errorMappings";
            if (errorMappings != null && errorMappings.Any())
            {
                hasErrorMappings = true;
                writer.WriteLine($"{errorMappingsVarName} = [");
                writer.IncreaseIndent(2);
                errorMappings.ToList().ForEach(errorMapping =>
                {
                    writer.WriteLine($"'{errorMapping.Key}' => [{errorMapping.Value.Name}::class, '{CreateDiscriminatorMethodName}'],");
                });
                writer.DecreaseIndent();
                writer.WriteLine("];");
            }

            var returnsVoid = returnType.Equals("void", StringComparison.OrdinalIgnoreCase);
            var isStream = returnType.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase);
            var isCollection = codeElement.ReturnType.IsCollection;
            var methodName = GetSendRequestMethodName(returnsVoid, isStream, isCollection, returnType);
            var returnTypeFactory = codeElement.ReturnType is CodeType {TypeDefinition: CodeClass}
                ? $", [{returnType}::class, '{CreateDiscriminatorMethodName}']"
                : string.Empty;
            var returnWithCustomType =
                !returnsVoid && string.IsNullOrEmpty(returnTypeFactory) && conventions.CustomTypes.Contains(returnType)
                    ? $", {returnType}::class"
                    : returnTypeFactory;
            var finalReturn = string.IsNullOrEmpty(returnWithCustomType) && !returnsVoid
                ? $", '{returnType}'"
                : returnWithCustomType;
            writer.WriteLine(
                $"return {GetPropertyCall(requestAdapterProperty, string.Empty)}->{methodName}({RequestInfoVarName}{finalReturn}, {(hasErrorMappings ? $"{errorMappingsVarName}" : "null")});");

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
            var pathParametersProperty = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
            WriteSerializationRegistration(codeMethod.SerializerModules, writer, "registerDefaultSerializer");
            WriteSerializationRegistration(codeMethod.DeserializerModules, writer, "registerDefaultDeserializer");
            if(!string.IsNullOrEmpty(codeMethod.BaseUrl)) {
                writer.WriteLines($"if (empty({GetPropertyCall(requestAdapterProperty, string.Empty)}->getBaseUrl())) {{");
                writer.IncreaseIndent();
                writer.WriteLine($"{GetPropertyCall(requestAdapterProperty, string.Empty)}->setBaseUrl('{codeMethod.BaseUrl}');");
                writer.CloseBlock();
                if (pathParametersProperty != null)
                    writer.WriteLine($"{GetPropertyCall(pathParametersProperty, default)}['baseUrl'] = {GetPropertyCall(requestAdapterProperty, string.Empty)}->getBaseUrl();");
            }
            var backingStoreParam = codeMethod.Parameters.OfKind(CodeParameterKind.BackingStore);
            if (backingStoreParam != null)
                writer.WriteLine($"{GetPropertyCall(requestAdapterProperty, string.Empty)}->enableBackingStore(${backingStoreParam.Name} ?? BackingStoreFactorySingleton::getInstance());");
        }
        
        private static void WriteSerializationRegistration(HashSet<string> serializationModules, LanguageWriter writer, string methodName) {
            if(serializationModules != null)
                foreach(var module in serializationModules)
                    writer.WriteLine($"ApiClientBuilder::{methodName}({module}::class);");
        }
        
        protected string GetSendRequestMethodName(bool isVoid, bool isStream, bool isCollection, string returnType)
        {
            if (isVoid) return "sendNoContentAsync";
            if (isStream || conventions.PrimitiveTypes.Contains(returnType.ToLowerInvariant()))
                if (isCollection)
                    return "sendPrimitiveCollectionAsync";
                else
                    return "sendPrimitiveAsync";
            if (isCollection) return "sendCollectionAsync";
            return "sendAsync";
        }

        private const int MaxDiscriminatorsPerMethod = 500;
        private const string DiscriminatorMappingVarName = "$mappingValue";
        private const string ResultVarName = "$result";

        private void WriteFactoryMethodBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
        {
            var parseNodeParameter = codeElement.Parameters.OfKind(CodeParameterKind.ParseNode);
            
            if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType || parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
                writer.WriteLine($"{ResultVarName} = new {codeElement.Parent.Name.ToFirstCharacterUpperCase()}();");
            var writeDiscriminatorValueRead = parentClass.DiscriminatorInformation.ShouldWriteParseNodeCheck && !parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType;

            if (writeDiscriminatorValueRead)
            {
                writer.WriteLines($"$mappingValueNode = ${parseNodeParameter.Name.ToFirstCharacterLowerCase()}->getChildNode(\"{parentClass.DiscriminatorInformation.DiscriminatorPropertyName}\");",
                    "if ($mappingValueNode !== null) {");
                writer.IncreaseIndent();
                writer.WriteLines($"{DiscriminatorMappingVarName} = $mappingValueNode->getStringValue();");
            }

            if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForInheritedType)
                WriteFactoryMethodBodyForInheritedModel(parentClass.DiscriminatorInformation.DiscriminatorMappings, writer, codeElement);
            else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType && parentClass.DiscriminatorInformation.HasBasicDiscriminatorInformation)
                WriteFactoryMethodBodyForUnionModelForDiscriminatedTypes(codeElement, parentClass, writer);
            else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
                WriteFactoryMethodBodyForIntersectionModel(codeElement, parentClass, writer);
           
            if(writeDiscriminatorValueRead) {
                writer.CloseBlock();
            }
            if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType || parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType) {
                if(parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType)
                    WriteFactoryMethodBodyForUnionModelForUnDiscriminatedTypes(codeElement, parentClass, writer);
                writer.WriteLine($"return {ResultVarName};");
            } else
                writer.WriteLine($"return new {codeElement.Parent.Name.ToFirstCharacterUpperCase()}();");
        }

        private void WriteFactoryMethodBodyForIntersectionModel(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
        { 
            var includeElse = false;
            var otherProps = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                    .Where(static x => x.Type is not CodeType propertyType || propertyType.IsCollection || propertyType.TypeDefinition is not CodeClass)
                                    .Order(CodePropertyTypeBackwardComparer)
                                    .ThenBy(static x => x.Name)
                                    .ToArray(); 
            foreach(var property in otherProps) { 
                if(property.Type is CodeType propertyType) { 
                    var deserializationMethodName = $"{ParseNodeVarName}->{GetDeserializationMethodName(propertyType, codeElement)}"; 
                    writer.StartBlock($"{(includeElse? "} else " : string.Empty)}if ({deserializationMethodName} !== null) {{"); 
                    writer.WriteLine($"{ResultVarName}->{property.Setter.Name.ToFirstCharacterLowerCase()}({deserializationMethodName});"); 
                    writer.DecreaseIndent();
                } 
                if(!includeElse)
                    includeElse = true;
            } 
            var complexProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                            .Select(static x => new Tuple<CodeProperty, CodeType>(x, x.Type as CodeType))
                                            .Where(static x => x.Item2.TypeDefinition is CodeClass && !x.Item2.IsCollection)
                                            .ToArray();
            if(complexProperties.Any()) {
                if(includeElse)
                    writer.StartBlock("} else {");
                foreach(var property in complexProperties)
                    writer.WriteLine($"{ResultVarName}->{property.Item1.Setter.Name.ToFirstCharacterLowerCase()}(new {conventions.GetTypeString(property.Item2, codeElement, false)}());");
                if(includeElse)
                    writer.CloseBlock();
            } else if (otherProps.Any())
                writer.CloseBlock(decreaseIndent: false);
        }

        private void WriteFactoryMethodBodyForUnionModelForDiscriminatedTypes(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
        {
            var includeElse = false;
            var otherProps = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                .Where(static x => x.Type is CodeType { IsCollection: false, TypeDefinition: CodeClass or CodeInterface })
                .Order(CodePropertyTypeForwardComparer)
                .ThenBy(static x => x.Name)
                .ToArray();
            foreach(var property in otherProps) {
                var propertyType = property.Type as CodeType;
                if (propertyType.TypeDefinition is CodeInterface { OriginalClass: { } } typeInterface)
                    propertyType = new CodeType {
                        Name = typeInterface.OriginalClass.Name,
                        TypeDefinition = typeInterface.OriginalClass,
                        CollectionKind = propertyType.CollectionKind,
                        IsNullable = propertyType.IsNullable,
                    };
                var mappedType = parentClass.DiscriminatorInformation.DiscriminatorMappings.FirstOrDefault(x => x.Value.Name.Equals(propertyType.Name, StringComparison.OrdinalIgnoreCase));
                writer.StartBlock($"{(includeElse? "} else " : string.Empty)}if ('{mappedType.Key}' === {DiscriminatorMappingVarName}) {{");
                writer.WriteLine($"{ResultVarName}->{property.Setter.Name.ToFirstCharacterLowerCase()}(new {conventions.GetTypeString(propertyType, codeElement, false)}());");
                writer.DecreaseIndent();
                if(!includeElse)
                    includeElse = true;
            }
            if(otherProps.Any())
                writer.CloseBlock(decreaseIndent: false);
        }

        private static readonly CodePropertyTypeComparer CodePropertyTypeForwardComparer = new();
        private static readonly CodePropertyTypeComparer CodePropertyTypeBackwardComparer = new(true);
        private void WriteFactoryMethodBodyForUnionModelForUnDiscriminatedTypes(CodeMethod currentElement, CodeClass parentClass, LanguageWriter writer)
        {
            var includeElse = false;
            var otherProps = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                .Where(static x => x.Type is CodeType xType && (xType.IsCollection || xType.TypeDefinition is null or CodeEnum))
                .Order(CodePropertyTypeForwardComparer)
                .ThenBy(static x => x.Name)
                .ToArray();
            foreach(var property in otherProps) {
                var propertyType = property.Type as CodeType;
                var serializationMethodName = $"{ParseNodeVarName}->{GetDeserializationMethodName(propertyType, currentElement)}";
                writer.StartBlock($"{(includeElse? "} else " : string.Empty)}if ({serializationMethodName} !== null) {{");
                writer.WriteLine($"{ResultVarName}->{property.Setter.Name.ToFirstCharacterLowerCase()}({serializationMethodName});");
                writer.DecreaseIndent();
                if(!includeElse)
                    includeElse = true;
            }
            if(otherProps.Any())
                writer.CloseBlock(decreaseIndent: false);
        }

        private void WriteFactoryMethodBodyForInheritedModel(IOrderedEnumerable<KeyValuePair<string, CodeTypeBase>> discriminatorMappings, LanguageWriter writer, CodeMethod method, string varName = default)
        {
            if (string.IsNullOrEmpty(varName))
                varName = DiscriminatorMappingVarName;
            writer.StartBlock($"switch ({varName}) {{");
            foreach(var mappedType in discriminatorMappings) {
                writer.WriteLine($"case '{mappedType.Key}': return new {conventions.GetTypeString(mappedType.Value.AllTypes.First(), method, false, writer)}();");
            }
            writer.CloseBlock();
        }
    }
}
