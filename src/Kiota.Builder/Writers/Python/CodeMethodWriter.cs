using System;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Python;
public class CodeMethodWriter : BaseElementWriter<CodeMethod, PythonConventionService>
{
    public CodeMethodWriter(PythonConventionService conventionService, bool usesBackingStore) : base(conventionService){
        _usesBackingStore = usesBackingStore;
    }
    private readonly bool _usesBackingStore;
    public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        if(codeElement.ReturnType == null) throw new InvalidOperationException($"{nameof(codeElement.ReturnType)} should not be null");
        ArgumentNullException.ThrowIfNull(writer);
        if(codeElement.Parent is not CodeClass parentClass) throw new InvalidOperationException("the parent of a method should be a class");

        var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement, true, writer);
        var isVoid = "None".Equals(returnType, StringComparison.OrdinalIgnoreCase);
        WriteMethodPrototype(codeElement, writer, returnType, isVoid);
        writer.IncreaseIndent();
        WriteMethodDocumentation(codeElement, writer, returnType, isVoid);
        var inherits = parentClass.StartBlock.Inherits != null && !parentClass.IsErrorDefinition;
        var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
        var requestConfigParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestConfiguration);
        var requestParams = new RequestParams(requestBodyParam, requestConfigParam);
        if(!codeElement.IsOfKind(CodeMethodKind.Setter))
            foreach(var parameter in codeElement.Parameters.Where(static x => !x.Optional).OrderBy(static x => x.Name)) {
                var parameterName = parameter.Name.ToSnakeCase();
                writer.StartBlock($"if {parameterName} is None:");
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
                WriteRequestExecutorBody(codeElement, requestParams, parentClass, isVoid, returnType, writer);
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
                throw new InvalidOperationException("RawUrlConstructor is not supported in python");
            case CodeMethodKind.RequestBuilderBackwardCompatibility:
                throw new InvalidOperationException("RequestBuilderBackwardCompatibility is not supported as the request builders are implemented by properties.");
            default:
                WriteDefaultMethodBody(codeElement, writer, returnType);
                break;
        }
        writer.CloseBlock(string.Empty);
    }
    private void WriteIndexerBody(CodeMethod codeElement, CodeClass parentClass, string returnType, LanguageWriter writer) {
        if (parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProperty &&
            codeElement.OriginalIndexer != null)
            conventions.AddParametersAssignment(writer, pathParametersProperty.Type, $"self.{pathParametersProperty.Name}",
                (codeElement.OriginalIndexer.IndexType, codeElement.OriginalIndexer.SerializationName, "id"));
        conventions.AddRequestBuilderBody(parentClass, returnType, writer, conventions.TempDictionaryVarName);
    }
    private void WriteRequestBuilderWithParametersBody(CodeMethod codeElement, CodeClass parentClass, string returnType, LanguageWriter writer)
    {
        var codePathParameters = codeElement.Parameters
                                                    .Where(x => x.IsOfKind(CodeParameterKind.Path));
        conventions.AddRequestBuilderBody(parentClass, returnType, writer, pathParameters: codePathParameters);
    }
    private static void WriteApiConstructorBody(CodeClass parentClass, CodeMethod method, LanguageWriter writer) {
        if(parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is not CodeProperty requestAdapterProperty) return;
        var backingStoreParameter = method.Parameters.OfKind(CodeParameterKind.BackingStore);
        var requestAdapterPropertyName = requestAdapterProperty.Name.ToSnakeCase();
        WriteSerializationRegistration(method.SerializerModules, writer, "register_default_serializer");
        WriteSerializationRegistration(method.DeserializerModules, writer, "register_default_deserializer");
        if(!string.IsNullOrEmpty(method.BaseUrl)) {
            writer.WriteLine($"if not {requestAdapterPropertyName}.base_url:");
            writer.IncreaseIndent();
            writer.WriteLine($"{requestAdapterPropertyName}.base_url = \"{method.BaseUrl}\"");
            writer.DecreaseIndent();
        }
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
            writer.WriteLine($"if {parameterName} == \"{escapedProperty.Name.ToSnakeCase()}\":");
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
    private CodePropertyKind[]? _DirectAccessProperties;
    private CodePropertyKind[] DirectAccessProperties { get {
        if(_DirectAccessProperties == null) {
            var directAccessProperties = new List<CodePropertyKind> {
                CodePropertyKind.BackingStore,
                CodePropertyKind.RequestBuilder,
                CodePropertyKind.UrlTemplate,
                CodePropertyKind.PathParameters
            };
            if(!_usesBackingStore) {
                directAccessProperties.Add(CodePropertyKind.AdditionalData);
            }
            _DirectAccessProperties = directAccessProperties.ToArray();
        }
        return _DirectAccessProperties;
    }}
    private CodePropertyKind[]? _SetterAccessProperties;
    private CodePropertyKind[] SetterAccessProperties {
        get {
            _SetterAccessProperties ??= new CodePropertyKind[] {
                    CodePropertyKind.AdditionalData, //additional data and custom properties need to use the accessors in case of backing store use
                    CodePropertyKind.Custom
                }.Except(DirectAccessProperties)
                .ToArray();
            return _SetterAccessProperties;
        }
    }
    private void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer, bool inherits) {
        if(inherits)
            writer.WriteLine("super().__init__()");
        WriteDirectAccessProperties(parentClass, writer);
        WriteSetterAccessProperties(parentClass, writer);
        WriteSetterAccessPropertiesWithoutDefaults(parentClass, writer);

        if(parentClass.IsOfKind(CodeClassKind.RequestBuilder)) {
            if(currentMethod.IsOfKind(CodeMethodKind.Constructor)) {
                if (currentMethod.Parameters.OfKind(CodeParameterKind.PathParameters) is CodeParameter pathParametersParam)
                    conventions.AddParametersAssignment(writer, 
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
    private void WriteDirectAccessProperties(CodeClass parentClass, LanguageWriter writer) {
        foreach(var propWithDefault in parentClass.GetPropertiesOfKind(DirectAccessProperties)
                                        .Where(static x => !string.IsNullOrEmpty(x.DefaultValue))
                                        .OrderByDescending(static x => x.Kind)
                                        .ThenBy(static x => x.Name)) {
            var returnType = conventions.GetTypeString(propWithDefault.Type, propWithDefault, true, writer);
            conventions.WriteInLineDescription(propWithDefault.Documentation.Description, writer);
            writer.WriteLine($"self.{conventions.GetAccessModifier(propWithDefault.Access)}{propWithDefault.NamePrefix}{propWithDefault.Name.ToSnakeCase()}: {(propWithDefault.Type.IsNullable ? "Optional[" : string.Empty)}{returnType}{(propWithDefault.Type.IsNullable ? "]" : string.Empty)} = {propWithDefault.DefaultValue}");
            writer.WriteLine();
        }
    }
    private void WriteSetterAccessProperties(CodeClass parentClass, LanguageWriter writer) {
        foreach(var propWithDefault in parentClass.GetPropertiesOfKind(SetterAccessProperties)
                                        .Where(static x => !string.IsNullOrEmpty(x.DefaultValue))
                                        // do not apply the default value if the type is composed as the default value may not necessarily which type to use
                                        .Where(static x => x.Type is not CodeType propType || propType.TypeDefinition is not CodeClass propertyClass || propertyClass.OriginalComposedType is null)
                                        .OrderByDescending(static x => x.Kind)
                                        .ThenBy(static x => x.Name)) {                                
            writer.WriteLine($"self.{propWithDefault.Name.ToSnakeCase()} = {propWithDefault.DefaultValue}");
        }
    }
    private void WriteSetterAccessPropertiesWithoutDefaults(CodeClass parentClass, LanguageWriter writer) {
        foreach(var propWithoutDefault in parentClass.GetPropertiesOfKind(SetterAccessProperties)
                                        .Where(static x => string.IsNullOrEmpty(x.DefaultValue))
                                        .OrderByDescending(static x => x.Kind)
                                        .ThenBy(static x => x.Name)) {
            var returnType = conventions.GetTypeString(propWithoutDefault.Type, propWithoutDefault, true, writer);
            conventions.WriteInLineDescription(propWithoutDefault.Documentation.Description, writer);
            writer.WriteLine($"self.{conventions.GetAccessModifier(propWithoutDefault.Access)}{propWithoutDefault.NamePrefix}{propWithoutDefault.Name.ToSnakeCase()}: {(propWithoutDefault.Type.IsNullable ? "Optional[" : string.Empty)}{returnType}{(propWithoutDefault.Type.IsNullable ? "]" : string.Empty)} = None");
        }
    }
    private static void AssignPropertyFromParameter(CodeClass parentClass, CodeMethod currentMethod, CodeParameterKind parameterKind, CodePropertyKind propertyKind, LanguageWriter writer, string? variableName = default) {
        if(parentClass.GetPropertyOfKind(propertyKind) is CodeProperty property) {
            if(!string.IsNullOrEmpty(variableName))
                writer.WriteLine($"self.{property.Name.ToSnakeCase()} = {variableName.ToSnakeCase()}");
            else if(currentMethod.Parameters.OfKind(parameterKind) is CodeParameter parameter)
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
                writer.WriteLines($"value: {conventions.GetTypeString(codeElement.AccessedProperty.Type, codeElement, true, writer)} = self.{backingStore.NamePrefix}{backingStore.Name.ToSnakeCase()}.get(\"{codeElement.AccessedProperty.Name.ToSnakeCase()}\")",
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
            foreach(var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom).Where(static x => !x.ExistsInBaseType)) {
                writer.WriteLine($"\"{otherProp.WireName}\": lambda n : setattr(self, '{otherProp.Name.ToSnakeCase()}', n.{GetDeserializationMethodName(otherProp.Type, codeElement, parentClass)}),");
            }
            writer.DecreaseIndent();
            writer.WriteLine("}");
            if (inherits) {
                writer.WriteLine($"super_fields = super().{codeElement.Name.ToSnakeCase()}()");
                writer.WriteLine("fields.update(super_fields)");
            }
            writer.WriteLine("return fields");
    }
    private void WriteRequestExecutorBody(CodeMethod codeElement, RequestParams requestParams, CodeClass parentClass, bool isVoid, string returnType, LanguageWriter writer) {
        if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");

        var generatorMethodName = parentClass
                                            .Methods
                                            .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod)
                                            ?.Name
                                            ?.ToSnakeCase();
        writer.WriteLine($"request_info = self.{generatorMethodName}(");
        var requestInfoParameters = new[] { requestParams.requestBody, requestParams.requestConfiguration }
                                        .Select(x => x?.Name.ToSnakeCase()).Where(x => x != null);
        if(requestInfoParameters.Any() && requestInfoParameters.Aggregate(static (x,y) => $"{x}, {y}") is string parameters) {
            writer.IncreaseIndent();
            writer.WriteLine(parameters);
            writer.DecreaseIndent();
        }
        writer.WriteLine(")");
        var isStream = conventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase);
        var returnTypeWithoutCollectionSymbol = GetReturnTypeWithoutCollectionSymbol(codeElement, returnType);
        var genericTypeForSendMethod = GetSendRequestMethodName(isVoid, isStream, codeElement.ReturnType.IsCollection, returnTypeWithoutCollectionSymbol);
        var newFactoryParameter = GetTypeFactory(isVoid, isStream, returnTypeWithoutCollectionSymbol);
        var errorMappingVarName = "None";
    if(codeElement.ErrorMappings.Any()) {
        errorMappingVarName = "error_mapping";
        writer.WriteLine($"{errorMappingVarName}: Dict[str, ParsableFactory] = {{");
        writer.IncreaseIndent();
        foreach(var errorMapping in codeElement.ErrorMappings) {
            writer.WriteLine($"\"{errorMapping.Key.ToUpperInvariant()}\": {errorMapping.Value.Name.ToSnakeCase()}.{errorMapping.Value.Name},");
        }
        writer.CloseBlock();
    }
    writer.WriteLine("if not self.request_adapter:");
    writer.IncreaseIndent();
    writer.WriteLine("raise Exception(\"Http core is null\") ");
    writer.DecreaseIndent();
    writer.WriteLine($"return await self.request_adapter.{genericTypeForSendMethod}(request_info,{newFactoryParameter} {errorMappingVarName})");
    }
    private string GetReturnTypeWithoutCollectionSymbol(CodeMethod codeElement, string fullTypeName) {
        if(!codeElement.ReturnType.IsCollection) return fullTypeName;
        if (codeElement.ReturnType.Clone() is CodeTypeBase clone) {
            clone.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.None;
            return conventions.GetTypeString(clone, codeElement);
        }
        return string.Empty;
    }
    private const string RequestInfoVarName = "request_info";
    private void WriteRequestGeneratorBody(CodeMethod codeElement, RequestParams requestParams, CodeClass currentClass, LanguageWriter writer) {
        if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
        
        writer.WriteLine($"{RequestInfoVarName} = RequestInformation()");
        if (currentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty urlTemplateParamsProperty &&
            currentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate) is CodeProperty urlTemplateProperty)
                writer.WriteLines($"{RequestInfoVarName}.url_template = {GetPropertyCall(urlTemplateProperty, "''")}",
                                    $"{RequestInfoVarName}.path_parameters = {GetPropertyCall(urlTemplateParamsProperty, "''")}");
        writer.WriteLine($"{RequestInfoVarName}.http_method = Method.{codeElement.HttpMethod.Value.ToString().ToUpperInvariant()}");
        if(codeElement.AcceptedResponseTypes.Any())
            writer.WriteLine($"{RequestInfoVarName}.headers[\"Accept\"] = \"{string.Join(", ", codeElement.AcceptedResponseTypes)}\"");
        UpdateRequestInformationFromRequestConfiguration(requestParams, writer);
        if(currentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is CodeProperty requestAdapterProperty)
            UpdateRequestInformationFromRequestBody(codeElement, requestParams, requestAdapterProperty, writer);
        writer.WriteLine($"return {RequestInfoVarName}");
    }
    private static string GetPropertyCall(CodeProperty property, string defaultValue) => property == null ? defaultValue : $"self.{property.Name.ToSnakeCase()}";
    private void WriteSerializerBody(bool inherits, CodeClass parentClass, LanguageWriter writer) {
        var additionalDataProperty = parentClass.GetPropertyOfKind(CodePropertyKind.AdditionalData);
        if(inherits)
            writer.WriteLine("super().serialize(writer)");
        foreach(var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom).Where(static x => !x.ExistsInBaseType && !x.ReadOnly)) {
            writer.WriteLine($"writer.{GetSerializationMethodName(otherProp.Type)}(\"{otherProp.WireName}\", self.{otherProp.Name.ToSnakeCase()})");
        }
        if(additionalDataProperty != null)
            writer.WriteLine($"writer.write_additional_data_value(self.{additionalDataProperty.Name.ToSnakeCase()})");
    }
    private void WriteMethodDocumentation(CodeMethod code, LanguageWriter writer, string returnType, bool isVoid) {
        var isDescriptionPresent = !string.IsNullOrEmpty(code.Documentation.Description);
        var parametersWithDescription = code.Parameters.Where(_ => !string.IsNullOrEmpty(code.Documentation.Description));
        var nullablePrefix = code.ReturnType.IsNullable && !isVoid ? "Optional[" : string.Empty;
        var nullableSuffix = code.ReturnType.IsNullable && !isVoid ? "]" : string.Empty;
        if (isDescriptionPresent || parametersWithDescription.Any()) {
            writer.WriteLine(conventions.DocCommentStart);
            if(isDescriptionPresent)
                writer.WriteLine($"{conventions.DocCommentPrefix}{PythonConventionService.RemoveInvalidDescriptionCharacters(code.Documentation.Description)}");
            if(parametersWithDescription.Any()) {
                writer.WriteLine("Args:");
                writer.IncreaseIndent();
                
                foreach(var paramWithDescription in parametersWithDescription.OrderBy(x => x.Name))
                    writer.WriteLine($"{conventions.DocCommentPrefix}{paramWithDescription.Name}: {PythonConventionService.RemoveInvalidDescriptionCharacters(paramWithDescription.Documentation.Description)}");
                writer.DecreaseIndent();
            }
            if(!isVoid)
                writer.WriteLine($"{conventions.DocCommentPrefix}Returns: {nullablePrefix}{returnType}{nullableSuffix}");
            writer.WriteLine(conventions.DocCommentEnd);
        }
    }
    private static readonly PythonCodeParameterOrderComparer parameterOrderComparer = new();
    private void WriteMethodPrototype(CodeMethod code, LanguageWriter writer, string returnType, bool isVoid) {
        if (code.IsOfKind(CodeMethodKind.Factory))
            writer.WriteLine("@staticmethod");
        var accessModifier = conventions.GetAccessModifier(code.Access);
        var isConstructor = code.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor);
        var methodName = (code.Kind switch {
            _ when code.IsAccessor => code.AccessedProperty?.Name,
            _ when isConstructor => "__init__",
            _ => code.Name,
        })?.ToSnakeCase();
        var asyncPrefix = code.IsAsync && code.Kind is CodeMethodKind.RequestExecutor ? "async ": string.Empty;
        var instanceReference = code.IsOfKind(CodeMethodKind.Factory) ? string.Empty: "self,";
        var parameters = string.Join(", ", code.Parameters.OrderBy(x => x, parameterOrderComparer)
                                                        .Select(p=> new PythonConventionService() // requires a writer instance because method parameters use inline type definitions
                                                        .GetParameterSignature(p, code, writer))
                                                        .ToList());
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
    private string GetDeserializationMethodName(CodeTypeBase propType, CodeMethod codeElement, CodeClass parentClass) {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyType = conventions.TranslateType(propType);
        if (conventions.TypeExistInSameClassAsTarget(propType, codeElement))
            propertyType = parentClass.Name.ToFirstCharacterUpperCase();
        if(propType is CodeType currentType)
        {
            if(currentType.TypeDefinition is CodeEnum currentEnum)
                return $"get_{(currentEnum.Flags || isCollection ? "collection_of_enum_values" : "enum_value")}({propertyType.ToCamelCase()})";
            if(isCollection)
                if(currentType.TypeDefinition == null)
                    return $"get_collection_of_primitive_values({propertyType.ToSnakeCase()})";
                else
                    return $"get_collection_of_object_values({propertyType.ToCamelCase()})";
        }
        return propertyType switch
        {
            "str" or "bool" or "int" or "float" or "UUID" or "date" or "time" or "datetime" or "timedelta" => $"get_{propertyType.ToSnakeCase()}_value()",
            "bytes" => "get_bytes_value()",
            _ => $"get_object_value({propertyType.ToCamelCase()})",
        };
    }
    private string GetSerializationMethodName(CodeTypeBase propType) {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyType = conventions.TranslateType(propType);
        if(propType is CodeType currentType)
        {
            if(currentType.TypeDefinition is CodeEnum)
                return "write_enum_value";
            if(isCollection)
                if(currentType.TypeDefinition == null)
                    return "write_collection_of_primitive_values";
                else
                    return "write_collection_of_object_values";
        }
        return propertyType switch
        {
            "str" or "bool" or "int" or "float" or "UUID" or "date" or "time" or "datetime" or "timedelta" => $"write_{propertyType.ToSnakeCase()}_value",
            _ => "write_object_value",
        };
    }
    private string GetTypeFactory(bool isVoid, bool isStream, string returnType)
    {
        if(isVoid) return string.Empty;
        if(isStream || conventions.IsPrimitiveType(returnType)) return $" \"{returnType}\",";
        return $" {returnType},";
    }
    private string GetSendRequestMethodName(bool isVoid, bool isStream, bool isCollection, string returnType)
    {
        if(isVoid) return "send_no_response_content_async";
        if(isCollection)
        {
            if(conventions.IsPrimitiveType(returnType)) return "send_collection_of_primitive_async";
            return $"send_collection_async";
        }

        if(isStream || conventions.IsPrimitiveType(returnType)) return "send_primitive_async";
        return "send_async";
    }

    private static void UpdateRequestInformationFromRequestConfiguration(RequestParams requestParams, LanguageWriter writer)
    {
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
    }

    private void UpdateRequestInformationFromRequestBody(CodeMethod codeElement, RequestParams requestParams, CodeProperty requestAdapterProperty, LanguageWriter writer)
    {
        if(requestParams.requestBody != null) {
            if(requestParams.requestBody.Type.Name.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
                writer.WriteLine($"{RequestInfoVarName}.set_stream_content({requestParams.requestBody.Name.ToSnakeCase()})");
            else {
                var setMethodName = requestParams.requestBody.Type is CodeType bodyType && bodyType.TypeDefinition is CodeClass ? "set_content_from_parsable" : "set_content_from_scalar";
                writer.WriteLine($"{RequestInfoVarName}.{setMethodName}(self.{requestAdapterProperty.Name.ToSnakeCase()}, \"{codeElement.RequestBodyContentType}\", {requestParams.requestBody.Name})");
            }
        }
    }
}
