using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Extensions;

namespace Kiota.Builder.Writers.TypeScript;
public class CodeMethodWriter : BaseElementWriter<CodeMethod, TypeScriptConventionService>
{
    public CodeMethodWriter(TypeScriptConventionService conventionService) : base(conventionService){}
    private TypeScriptConventionService localConventions;
    public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
    {
        if(codeElement == null) throw new ArgumentNullException(nameof(codeElement));
        if(codeElement.ReturnType == null) throw new InvalidOperationException($"{nameof(codeElement.ReturnType)} should not be null");
        if(writer == null) throw new ArgumentNullException(nameof(writer));
        if(codeElement.Parent is CodeFunction) return;
        if(!(codeElement.Parent is CodeClass)) throw new InvalidOperationException("the parent of a method should be a class");

        localConventions = new TypeScriptConventionService(writer); //because we allow inline type definitions for methods parameters
        var returnType = localConventions.GetTypeString(codeElement.ReturnType, codeElement);
        var isVoid = "void".Equals(returnType, StringComparison.OrdinalIgnoreCase);
        WriteMethodDocumentation(codeElement, writer, isVoid);
        WriteMethodPrototype(codeElement, writer, returnType, isVoid);
        writer.IncreaseIndent();
        var parentClass = codeElement.Parent as CodeClass;
        var inherits = parentClass.StartBlock is CodeClass.Declaration declaration && declaration.Inherits != null && !parentClass.IsErrorDefinition;
        var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
        var queryStringParam = codeElement.Parameters.OfKind(CodeParameterKind.QueryParameter);
        var headersParam = codeElement.Parameters.OfKind(CodeParameterKind.Headers);
        var optionsParam = codeElement.Parameters.OfKind(CodeParameterKind.Options);
        var requestParams = new RequestParams(requestBodyParam, queryStringParam, headersParam, optionsParam);
        WriteDefensiveStatements(codeElement, writer);
        switch(codeElement.Kind) {
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
            case CodeMethodKind.ClientConstructor:
                WriteConstructorBody(parentClass, codeElement, writer, inherits);
                WriteApiConstructorBody(parentClass, codeElement, writer);
            break;
            case CodeMethodKind.Constructor:
                WriteConstructorBody(parentClass, codeElement, writer, inherits);
                break;
            case CodeMethodKind.RequestBuilderWithParameters:
                WriteRequestBuilderWithParametersBody(codeElement, parentClass, returnType, writer);
                break;
            case CodeMethodKind.Factory:
                throw new InvalidOperationException("Factory methods are implemented as functions in TypeScript");
            case CodeMethodKind.RawUrlConstructor:
                throw new InvalidOperationException("RawUrlConstructor is not supported as typescript relies on union types.");
            case CodeMethodKind.RequestBuilderBackwardCompatibility:
                throw new InvalidOperationException("RequestBuilderBackwardCompatibility is not supported as the request builders are implemented by properties.");
            default:
                WriteDefaultMethodBody(codeElement, writer);
                break;
        }
        writer.DecreaseIndent();
        writer.WriteLine("};");
    }
    internal static void WriteDefensiveStatements(CodeMethod codeElement, LanguageWriter writer) {
        if(codeElement.IsOfKind(CodeMethodKind.Setter)) return;

        foreach(var parameter in codeElement.Parameters.Where(x => !x.Optional).OrderBy(x => x.Name)) {
            var parameterName = parameter.Name.ToFirstCharacterLowerCase();
            writer.WriteLine($"if(!{parameterName}) throw new Error(\"{parameterName} cannot be undefined\");");
        }
    }
    private void WriteIndexerBody(CodeMethod codeElement, CodeClass parentClass, string returnType, LanguageWriter writer) {
        var pathParametersProperty = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
        localConventions.AddParametersAssignment(writer, pathParametersProperty.Type, $"this.{pathParametersProperty.Name}",
            (codeElement.OriginalIndexer.IndexType, codeElement.OriginalIndexer.ParameterName, "id"));
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
        var requestAdapterPropertyName = requestAdapterProperty.Name.ToFirstCharacterLowerCase();
        WriteSerializationRegistration(method.SerializerModules, writer, "registerDefaultSerializer");
        WriteSerializationRegistration(method.DeserializerModules, writer, "registerDefaultDeserializer");
        writer.WriteLine($"{requestAdapterPropertyName}.baseUrl = \"{method.BaseUrl}\";");
        if(backingStoreParameter != null)
            writer.WriteLine($"this.{requestAdapterPropertyName}.enableBackingStore({backingStoreParameter.Name});");
    }
    private static void WriteSerializationRegistration(List<string> serializationModules, LanguageWriter writer, string methodName) {
        if(serializationModules != null)
            foreach(var module in serializationModules)
                writer.WriteLine($"{methodName}({module});");
    }
    private void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer, bool inherits) {
        if(inherits || parentClass.IsErrorDefinition)
            writer.WriteLine("super();");
        if(parentClass.IsErrorDefinition && parentClass.StartBlock is CodeClass.Declaration declaration)
            writer.WriteLine($"Object.setPrototypeOf(this, {declaration.Inherits.Name.ToFirstCharacterUpperCase()}.prototype);");
        var propertiesWithDefaultValues = new List<CodePropertyKind> {
            CodePropertyKind.AdditionalData,
            CodePropertyKind.BackingStore,
            CodePropertyKind.RequestBuilder,
            CodePropertyKind.UrlTemplate,
            CodePropertyKind.PathParameters,
        };
        foreach(var propWithDefault in parentClass.GetPropertiesOfKind(propertiesWithDefaultValues.ToArray())
                                        .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
                                        .OrderByDescending(x => x.Kind)
                                        .ThenBy(x => x.Name)) {
            writer.WriteLine($"this.{propWithDefault.NamePrefix}{propWithDefault.Name.ToFirstCharacterLowerCase()} = {propWithDefault.DefaultValue};");
        }
        if(parentClass.IsOfKind(CodeClassKind.RequestBuilder)) {
            if(currentMethod.IsOfKind(CodeMethodKind.Constructor)) {
                var pathParametersParam = currentMethod.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.PathParameters));
                localConventions.AddParametersAssignment(writer, 
                                                    pathParametersParam.Type.AllTypes.OfType<CodeType>().FirstOrDefault(),
                                                    pathParametersParam.Name.ToFirstCharacterLowerCase(),
                                                    currentMethod.Parameters
                                                                .Where(x => x.IsOfKind(CodeParameterKind.Path))
                                                                .Select(x => (x.Type, x.UrlTemplateParameterName, x.Name.ToFirstCharacterLowerCase()))
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
                writer.WriteLine($"this.{property.Name.ToFirstCharacterLowerCase()} = {variableName};");
            else if(parameter != null)
                writer.WriteLine($"this.{property.Name.ToFirstCharacterLowerCase()} = {parameter.Name};");
        }
    }
    private static void WriteSetterBody(CodeMethod codeElement, LanguageWriter writer, CodeClass parentClass) {
        var backingStore = parentClass.GetBackingStoreProperty();
        if(backingStore == null)
            writer.WriteLine($"this.{codeElement.AccessedProperty?.NamePrefix}{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()} = value;");
        else
            writer.WriteLine($"this.{backingStore.NamePrefix}{backingStore.Name.ToFirstCharacterLowerCase()}.set(\"{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()}\", value);");
    }
    private void WriteGetterBody(CodeMethod codeElement, LanguageWriter writer, CodeClass parentClass) {
        var backingStore = parentClass.GetBackingStoreProperty();
        if(backingStore == null)
            writer.WriteLine($"return this.{codeElement.AccessedProperty?.NamePrefix}{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()};");
        else 
            if(!(codeElement.AccessedProperty?.Type?.IsNullable ?? true) &&
                !(codeElement.AccessedProperty?.ReadOnly ?? true) &&
                !string.IsNullOrEmpty(codeElement.AccessedProperty?.DefaultValue)) {
                writer.WriteLines($"let value = this.{backingStore.NamePrefix}{backingStore.Name.ToFirstCharacterLowerCase()}.get<{conventions.GetTypeString(codeElement.AccessedProperty.Type, codeElement)}>(\"{codeElement.AccessedProperty.Name.ToFirstCharacterLowerCase()}\");",
                    "if(!value) {");
                writer.IncreaseIndent();
                writer.WriteLines($"value = {codeElement.AccessedProperty.DefaultValue};",
                    $"this.{codeElement.AccessedProperty?.NamePrefix}{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()} = value;");
                writer.DecreaseIndent();
                writer.WriteLines("}", "return value;");
            } else
                writer.WriteLine($"return this.{backingStore.NamePrefix}{backingStore.Name.ToFirstCharacterLowerCase()}.get(\"{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()}\");");

    }
    private static void WriteDefaultMethodBody(CodeMethod codeElement, LanguageWriter writer) {
        var promisePrefix = codeElement.IsAsync ? "Promise.resolve(" : string.Empty;
        var promiseSuffix = codeElement.IsAsync ? ")" : string.Empty;
        writer.WriteLine($"return {promisePrefix}{(codeElement.ReturnType.Name.Equals("string") ? "''" : "{} as any")}{promiseSuffix};");
    }
    private void WriteDeserializerBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer, bool inherits) {
        writer.WriteLine($"return new Map<string, (item: T, node: {localConventions.ParseNodeInterfaceName}) => void>([{(inherits ? $"...super.{codeElement.Name.ToFirstCharacterLowerCase()}()," : string.Empty)}");
        writer.IncreaseIndent();
        var parentClassName = parentClass.Name.ToFirstCharacterUpperCase();
        foreach(var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)) {
            writer.WriteLine($"[\"{otherProp.SerializationName ?? otherProp.Name.ToFirstCharacterLowerCase()}\", (o, n) => {{ (o as unknown as {parentClassName}).{otherProp.Name.ToFirstCharacterLowerCase()} = n.{GetDeserializationMethodName(otherProp.Type)}; }}],");
        }
        writer.DecreaseIndent();
        writer.WriteLine("]);");
    }
    private void WriteRequestExecutorBody(CodeMethod codeElement, RequestParams requestParams, bool isVoid, string returnType, LanguageWriter writer) {
        if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");

        var generatorMethodName = (codeElement.Parent as CodeClass)
                                            .Methods
                                            .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod)
                                            ?.Name
                                            ?.ToFirstCharacterLowerCase();
        writer.WriteLine($"const requestInfo = this.{generatorMethodName}(");
        var requestInfoParameters = new CodeParameter[] { requestParams.requestBody, requestParams.queryString, requestParams.headers, requestParams.options }
                                        .Select(x => x?.Name).Where(x => x != null);
        if(requestInfoParameters.Any()) {
            writer.IncreaseIndent();
            writer.WriteLine(requestInfoParameters.Aggregate((x,y) => $"{x}, {y}"));
            writer.DecreaseIndent();
        }
        writer.WriteLine(");");
        var isStream = localConventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase);
        var returnTypeWithoutCollectionSymbol = GetReturnTypeWithoutCollectionSymbol(codeElement, returnType);
        var genericTypeForSendMethod = GetSendRequestMethodName(isVoid, isStream, codeElement.ReturnType.IsCollection, returnTypeWithoutCollectionSymbol);
        var newFactoryParameter = GetTypeFactory(isVoid, isStream, returnTypeWithoutCollectionSymbol);
        var errorMappingVarName = "undefined";
        if(codeElement.ErrorMappings.Any()) {
            errorMappingVarName = "errorMapping";
            writer.WriteLine($"const {errorMappingVarName}: Record<string, ParsableFactory<Parsable>> = {{");
            writer.IncreaseIndent();
            foreach(var errorMapping in codeElement.ErrorMappings) {
                writer.WriteLine($"\"{errorMapping.Key.ToUpperInvariant()}\": {GetFactoryMethodName(errorMapping.Value.Name)},");
            }
            writer.CloseBlock("};");
        }
        writer.WriteLine($"return this.requestAdapter?.{genericTypeForSendMethod}(requestInfo,{newFactoryParameter} responseHandler, {errorMappingVarName}) ?? Promise.reject(new Error('http core is null'));");
    }
    private string GetReturnTypeWithoutCollectionSymbol(CodeMethod codeElement, string fullTypeName) {
        if(!codeElement.ReturnType.IsCollection) return fullTypeName;
        var clone = codeElement.ReturnType.Clone() as CodeTypeBase;
        clone.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.None;
        return conventions.GetTypeString(clone, codeElement);
    }
    private const string RequestInfoVarName = "requestInfo";
    private void WriteRequestGeneratorBody(CodeMethod codeElement, RequestParams requestParams, CodeClass currentClass, LanguageWriter writer) {
        if(codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
        
        var urlTemplateParamsProperty = currentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
        var urlTemplateProperty = currentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate);
        var requestAdapterProperty = currentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
        writer.WriteLines($"const {RequestInfoVarName} = new RequestInformation();",
                            $"{RequestInfoVarName}.urlTemplate = {GetPropertyCall(urlTemplateProperty, "''")};",
                            $"{RequestInfoVarName}.pathParameters = {GetPropertyCall(urlTemplateParamsProperty, "''")};",
                            $"{RequestInfoVarName}.httpMethod = HttpMethod.{codeElement.HttpMethod.ToString().ToUpperInvariant()};");
        if(requestParams.headers != null)
            writer.WriteLine($"if(h) {RequestInfoVarName}.headers = h;");
        if(requestParams.queryString != null)
            writer.WriteLines($"{requestParams.queryString.Name} && {RequestInfoVarName}.setQueryStringParametersFromRawObject(q);");
        if(requestParams.requestBody != null) {
            if(requestParams.requestBody.Type.Name.Equals(localConventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
                writer.WriteLine($"{RequestInfoVarName}.setStreamContent({requestParams.requestBody.Name});");
            else {
                var spreadOperator = requestParams.requestBody.Type.AllTypes.First().IsCollection ? "..." : string.Empty;
                writer.WriteLine($"{RequestInfoVarName}.setContentFromParsable(this.{requestAdapterProperty.Name.ToFirstCharacterLowerCase()}, \"{codeElement.ContentType}\", {spreadOperator}{requestParams.requestBody.Name});");
            }
        }
        if(requestParams.options != null)
            writer.WriteLine($"{requestParams.options.Name} && {RequestInfoVarName}.addRequestOptions(...{requestParams.options.Name});");
        writer.WriteLine($"return {RequestInfoVarName};");
    }
    private static string GetPropertyCall(CodeProperty property, string defaultValue) => property == null ? defaultValue : $"this.{property.Name}";
    private void WriteSerializerBody(bool inherits, CodeClass parentClass, LanguageWriter writer) {
        var additionalDataProperty = parentClass.GetPropertyOfKind(CodePropertyKind.AdditionalData);
        if(inherits)
            writer.WriteLine("super.serialize(writer);");
        foreach(var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)) {
            var isCollectionOfEnum = otherProp.Type is CodeType cType && cType.IsCollection && cType.TypeDefinition is CodeEnum;
            var spreadOperator = isCollectionOfEnum ? "..." : string.Empty;
            var otherPropName = otherProp.Name.ToFirstCharacterLowerCase();
            var undefinedPrefix = isCollectionOfEnum ? $"this.{otherPropName} && " : string.Empty;
            writer.WriteLine($"{undefinedPrefix}writer.{GetSerializationMethodName(otherProp.Type)}(\"{otherProp.SerializationName ?? otherPropName}\", {spreadOperator}this.{otherPropName});");
        }
        if(additionalDataProperty != null)
            writer.WriteLine($"writer.writeAdditionalData(this.{additionalDataProperty.Name.ToFirstCharacterLowerCase()});");
    }
    private void WriteMethodDocumentation(CodeMethod code, LanguageWriter writer, bool isVoid) {
        var isDescriptionPresent = !string.IsNullOrEmpty(code.Description);
        var parametersWithDescription = code.Parameters.Where(x => !string.IsNullOrEmpty(code.Description));
        if (isDescriptionPresent || parametersWithDescription.Any()) {
            writer.WriteLine(localConventions.DocCommentStart);
            if(isDescriptionPresent)
                writer.WriteLine($"{localConventions.DocCommentPrefix}{TypeScriptConventionService.RemoveInvalidDescriptionCharacters(code.Description)}");
            foreach(var paramWithDescription in parametersWithDescription.OrderBy(x => x.Name))
                writer.WriteLine($"{localConventions.DocCommentPrefix}@param {paramWithDescription.Name} {TypeScriptConventionService.RemoveInvalidDescriptionCharacters(paramWithDescription.Description)}");
            
            if(!isVoid)
                if(code.IsAsync)
                    writer.WriteLine($"{localConventions.DocCommentPrefix}@returns a Promise of {code.ReturnType.Name.ToFirstCharacterUpperCase()}");
                else
                    writer.WriteLine($"{localConventions.DocCommentPrefix}@returns a {code.ReturnType.Name}");
            writer.WriteLine(localConventions.DocCommentEnd);
        }
    }
    private static readonly CodeParameterOrderComparer parameterOrderComparer = new();
    private void WriteMethodPrototype(CodeMethod code, LanguageWriter writer, string returnType, bool isVoid) {
        WriteMethodPrototypeInternal(code, writer, returnType, isVoid, localConventions, false);
    }
    internal static void WriteMethodPrototypeInternal(CodeMethod code, LanguageWriter writer, string returnType, bool isVoid, TypeScriptConventionService pConventions, bool isFunction) {
        var accessModifier = isFunction ? string.Empty : pConventions.GetAccessModifier(code.Access);
        var isConstructor = code.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor);
        var methodName = (code.Kind switch {
            _ when code.IsAccessor => code.AccessedProperty?.Name,
            _ when isConstructor => "constructor",
            _ => code.Name,
        })?.ToFirstCharacterLowerCase();
        var asyncPrefix = code.IsAsync && code.Kind != CodeMethodKind.RequestExecutor ? " async ": string.Empty;
        var staticPrefix = code.IsStatic && !isFunction ? "static " : string.Empty;
        var functionPrefix = isFunction ? "export function " : " ";
        var parameters = string.Join(", ", code.Parameters.OrderBy(x => x, parameterOrderComparer).Select(p=> pConventions.GetParameterSignature(p, code)).ToList());
        var asyncReturnTypePrefix = code.IsAsync ? "Promise<": string.Empty;
        var asyncReturnTypeSuffix = code.IsAsync ? ">": string.Empty;
        var nullableSuffix = code.ReturnType.IsNullable && !isVoid ? " | undefined" : string.Empty;
        var accessorPrefix = code.Kind switch {
                CodeMethodKind.Getter => "get ",
                CodeMethodKind.Setter => "set ",
                _ => string.Empty
            };
        var shouldHaveTypeSuffix = !code.IsAccessor && !isConstructor;
        var returnTypeSuffix = shouldHaveTypeSuffix ? $" : {asyncReturnTypePrefix}{returnType}{nullableSuffix}{asyncReturnTypeSuffix}" : string.Empty;
        writer.WriteLine($"{accessModifier}{functionPrefix}{accessorPrefix}{staticPrefix}{methodName}{asyncPrefix}({parameters}){returnTypeSuffix} {{");
    }
    private string GetDeserializationMethodName(CodeTypeBase propType) {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyType = localConventions.TranslateType(propType);
        if(propType is CodeType currentType) {
            if(currentType.TypeDefinition is CodeEnum currentEnum)
                return $"getEnumValue{(currentEnum.Flags || isCollection ? "s" : string.Empty)}<{currentEnum.Name.ToFirstCharacterUpperCase()}>({propertyType.ToFirstCharacterUpperCase()})";
            else if(isCollection)
                if(currentType.TypeDefinition == null)
                    return $"getCollectionOfPrimitiveValues<{propertyType.ToFirstCharacterLowerCase()}>()";
                else
                    return $"getCollectionOfObjectValues<{propertyType.ToFirstCharacterUpperCase()}>({GetFactoryMethodName(propertyType)})";
        }
        return propertyType switch
        {
            "string" or "boolean" or "number" or "Guid" or "Date" or "DateOnly" or "TimeOnly" or "Duration" => $"get{propertyType.ToFirstCharacterUpperCase()}Value()",
            _ => $"getObjectValue<{propertyType.ToFirstCharacterUpperCase()}>({GetFactoryMethodName(propertyType)})",
        };
    }
    private static string GetFactoryMethodName(string targetClassName) =>
        $"create{targetClassName.ToFirstCharacterUpperCase()}FromDiscriminatorValue";
    private string GetSerializationMethodName(CodeTypeBase propType) {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyType = localConventions.TranslateType(propType);
        if(propType is CodeType currentType) {
            if(currentType.TypeDefinition is CodeEnum currentEnum)
                return $"writeEnumValue<{currentEnum.Name.ToFirstCharacterUpperCase()}>";
            else if(isCollection)
                if(currentType.TypeDefinition == null)
                    return $"writeCollectionOfPrimitiveValues<{propertyType.ToFirstCharacterLowerCase()}>";
                else
                    return $"writeCollectionOfObjectValues<{propertyType.ToFirstCharacterUpperCase()}>";
        }
        return propertyType switch
        {
            "string" or "boolean" or "number" or "Guid" or "Date" or "DateOnly" or "TimeOnly" or "Duration" => $"write{propertyType.ToFirstCharacterUpperCase()}Value",
            _ => $"writeObjectValue<{propertyType.ToFirstCharacterUpperCase()}>",
        };
    }
    private string GetTypeFactory(bool isVoid, bool isStream, string returnType) {
        if(isVoid) return string.Empty;
        else if(isStream || conventions.IsPrimitiveType(returnType)) return $" \"{returnType}\",";
        else return $" {GetFactoryMethodName(returnType)},";
    }
    private string GetSendRequestMethodName(bool isVoid, bool isStream, bool isCollection, string returnType) {
        if(isVoid) return "sendNoResponseContentAsync";
        else if(isCollection) {
            if(conventions.IsPrimitiveType(returnType)) return $"sendCollectionOfPrimitiveAsync<{returnType}>";
            else return $"sendCollectionAsync<{returnType}>";
        }
        else if(isStream || conventions.IsPrimitiveType(returnType)) return $"sendPrimitiveAsync<{returnType}>";
        else return $"sendAsync<{returnType}>";
    }
}
