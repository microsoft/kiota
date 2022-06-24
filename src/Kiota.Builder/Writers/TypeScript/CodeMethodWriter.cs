using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Extensions;

namespace Kiota.Builder.Writers.TypeScript;
public class CodeMethodWriter : BaseElementWriter<CodeMethod, TypeScriptConventionService>
{
    public CodeMethodWriter(TypeScriptConventionService conventionService) : base(conventionService) { }
    private TypeScriptConventionService localConventions;

    private const string ModelClassSuffix = "Impl";
    public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
    {
        if (codeElement == null) throw new ArgumentNullException(nameof(codeElement));
        if (codeElement.ReturnType == null) throw new InvalidOperationException($"{nameof(codeElement.ReturnType)} should not be null");
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        if (codeElement.Parent is CodeFunction) return;
        if (!(codeElement.Parent is CodeClass)) throw new InvalidOperationException("the parent of a method should be a class");

        localConventions = new TypeScriptConventionService(writer); //because we allow inline type definitions for methods parameters
        var returnType = localConventions.GetTypeString(codeElement.ReturnType, codeElement);
        var isVoid = "void".Equals(returnType, StringComparison.OrdinalIgnoreCase);
        WriteMethodDocumentation(codeElement, writer, isVoid);
        WriteMethodPrototype(codeElement, writer, returnType, isVoid);
        writer.IncreaseIndent();
        var parentClass = codeElement.Parent as CodeClass;
        var inherits = parentClass.StartBlock.Inherits != null && !parentClass.IsErrorDefinition;
        var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
        var requestConfigParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestConfiguration);
        var requestParams = new RequestParams(requestBodyParam, requestConfigParam);
        WriteDefensiveStatements(codeElement, writer);
        switch (codeElement.Kind)
        {
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
            case CodeMethodKind.QueryParametersMapper:
                WriteQueryParametersMapper(codeElement, parentClass, writer);
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

    private static void WriteQueryParametersMapper(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        var parameter = codeElement.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.QueryParametersMapperParameter));
        if (parameter == null) throw new InvalidOperationException("QueryParametersMapper should have a parameter of type QueryParametersMapper");
        var parameterName = parameter.Name.ToFirstCharacterLowerCase();
        writer.WriteLine($"switch({parameterName}) {{");
        writer.IncreaseIndent();
        var escapedProperties = parentClass.Properties.Where(x => x.IsOfKind(CodePropertyKind.QueryParameter) && x.IsNameEscaped);
        foreach (var escapedProperty in escapedProperties)
        {
            writer.WriteLine($"case \"{escapedProperty.Name}\": return \"{escapedProperty.SerializationName}\";");
        }
        writer.WriteLine($"default: return {parameterName};");
        writer.CloseBlock();
    }

    internal static void WriteDefensiveStatements(CodeMethod codeElement, LanguageWriter writer)
    {
        if (codeElement.IsOfKind(CodeMethodKind.Setter)) return;

        foreach (var parameter in codeElement.Parameters.Where(x => !x.Optional).OrderBy(x => x.Name))
        {
            var parameterName = parameter.Name.ToFirstCharacterLowerCase();
            writer.WriteLine($"if(!{parameterName}) throw new Error(\"{parameterName} cannot be undefined\");");
        }
    }
    private void WriteIndexerBody(CodeMethod codeElement, CodeClass parentClass, string returnType, LanguageWriter writer)
    {
        var pathParametersProperty = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
        localConventions.AddParametersAssignment(writer, pathParametersProperty.Type, $"this.{pathParametersProperty.Name}",
            (codeElement.OriginalIndexer.IndexType, codeElement.OriginalIndexer.SerializationName, "id"));
        localConventions.AddRequestBuilderBody(parentClass, returnType, writer, conventions.TempDictionaryVarName);
    }
    private void WriteRequestBuilderWithParametersBody(CodeMethod codeElement, CodeClass parentClass, string returnType, LanguageWriter writer)
    {
        var codePathParameters = codeElement.Parameters
                                                    .Where(x => x.IsOfKind(CodeParameterKind.Path));
        localConventions.AddRequestBuilderBody(parentClass, returnType, writer, pathParameters: codePathParameters);
    }
    private static void WriteApiConstructorBody(CodeClass parentClass, CodeMethod method, LanguageWriter writer)
    {
        var requestAdapterProperty = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
        var backingStoreParameter = method.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.BackingStore));
        var requestAdapterPropertyName = requestAdapterProperty.Name.ToFirstCharacterLowerCase();
        WriteSerializationRegistration(method.SerializerModules, writer, "registerDefaultSerializer");
        WriteSerializationRegistration(method.DeserializerModules, writer, "registerDefaultDeserializer");
        writer.WriteLine($"if ({requestAdapterPropertyName}.baseUrl === undefined || {requestAdapterPropertyName}.baseUrl === \"\") {{");
        writer.IncreaseIndent();
        writer.WriteLine($"{requestAdapterPropertyName}.baseUrl = \"{method.BaseUrl}\";");
        writer.CloseBlock();
        if (backingStoreParameter != null)
            writer.WriteLine($"this.{requestAdapterPropertyName}.enableBackingStore({backingStoreParameter.Name});");
    }
    private static void WriteSerializationRegistration(HashSet<string> serializationModules, LanguageWriter writer, string methodName)
    {
        if (serializationModules != null)
            foreach (var module in serializationModules)
                writer.WriteLine($"{methodName}({module});");
    }
    private void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer, bool inherits)
    {
        if (!parentClass.IsOfKind(CodeClassKind.Model) && (inherits || parentClass.IsErrorDefinition))
            writer.WriteLine("super();");
        var propertiesWithDefaultValues = new List<CodePropertyKind> {
            CodePropertyKind.BackingStore,
            CodePropertyKind.RequestBuilder,
            CodePropertyKind.UrlTemplate,
            CodePropertyKind.PathParameters,
        };
        foreach (var propWithDefault in parentClass.GetPropertiesOfKind(propertiesWithDefaultValues.ToArray())
                                        .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
                                        .OrderByDescending(x => x.Kind)
                                        .ThenBy(x => x.Name))
        {
            writer.WriteLine($"this.{propWithDefault.NamePrefix}{propWithDefault.Name.ToFirstCharacterLowerCase()} = {propWithDefault.DefaultValue};");
        }

        if (parentClass.IsOfKind(CodeClassKind.RequestBuilder))
        {
            if (currentMethod.IsOfKind(CodeMethodKind.Constructor))
            {
                var pathParametersParam = currentMethod.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.PathParameters));
                localConventions.AddParametersAssignment(writer,
                                                    pathParametersParam.Type.AllTypes.OfType<CodeType>().FirstOrDefault(),
                                                    pathParametersParam.Name.ToFirstCharacterLowerCase(),
                                                    currentMethod.Parameters
                                                                .Where(x => x.IsOfKind(CodeParameterKind.Path))
                                                                .Select(x => (x.Type, string.IsNullOrEmpty(x.SerializationName) ? x.Name : x.SerializationName, x.Name.ToFirstCharacterLowerCase()))
                                                                .ToArray());
                AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.PathParameters, CodePropertyKind.PathParameters, writer, conventions.TempDictionaryVarName);
            }
            AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.RequestAdapter, CodePropertyKind.RequestAdapter, writer);
        }

        if (parentClass.IsOfKind(CodeClassKind.Model))
        {
            ConstructorBodyForModelClass(parentClass, writer, currentMethod);
        }
    }

    private void ConstructorBodyForModelClass(CodeClass codeClass, LanguageWriter writer, CodeMethod currentMethod)
    {
        var codeInterfaceName = currentMethod.Parameters.FirstOrDefault(x => x.Type is CodeType type && type.TypeDefinition is CodeInterface).Name;
        if (codeClass.StartBlock.Inherits != null)
        {
            if (codeClass.StartBlock.Inherits.TypeDefinition != null)
            {
                writer.WriteLine($"super({codeInterfaceName});");
            }
            else
            {   
                // For Error Model Classes.
                writer.WriteLine($"super();");
            }
        }

        foreach (var prop in codeClass.Properties)
        {
            var interfaceProperty = $"{codeInterfaceName}?.{prop.Name.ToFirstCharacterLowerCase()}";
            var s = prop.Type is CodeType type && type.TypeDefinition is CodeInterface @interface? @interface.Name: "";
  
            if (prop.IsOfKind(CodePropertyKind.AdditionalData))
            {
                writer.WriteLine($"this.{prop.NamePrefix}{prop.Name.ToFirstCharacterLowerCase()} = {interfaceProperty} ? {interfaceProperty}! : {prop.DefaultValue};");
            }
            else
            {   
                var property = IsCodePropertyCollection(prop) ? ConvertPropertyValueToInstanceArray(prop.Name, prop.Type, writer) : (!String.IsNullOrWhiteSpace(s) ? $"{interfaceProperty} instanceof {s}{ModelClassSuffix}? {interfaceProperty}:new {s}{ModelClassSuffix}({interfaceProperty})":interfaceProperty );
                writer.WriteLine($"this.{prop.NamePrefix}{prop.Name.ToFirstCharacterLowerCase()} = {property};");
            }
        }
    }

    private static bool IsCodePropertyCollection(CodeProperty property)
    {
        return( property.Type.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None && property.Type is CodeType currentType && currentType.TypeDefinition != null);
    }
    private static void AssignPropertyFromParameter(CodeClass parentClass, CodeMethod currentMethod, CodeParameterKind parameterKind, CodePropertyKind propertyKind, LanguageWriter writer, string variableName = default)
    {
        var property = parentClass.GetPropertyOfKind(propertyKind);
        if (property != null)
        {
            var parameter = currentMethod.Parameters.FirstOrDefault(x => x.IsOfKind(parameterKind));
            if (!string.IsNullOrEmpty(variableName))
                writer.WriteLine($"this.{property.Name.ToFirstCharacterLowerCase()} = {variableName};");
            else if (parameter != null)
                writer.WriteLine($"this.{property.Name.ToFirstCharacterLowerCase()} = {parameter.Name};");
        }
    }
    private static void WriteSetterBody(CodeMethod codeElement, LanguageWriter writer, CodeClass parentClass)
    {
        var backingStore = parentClass.GetBackingStoreProperty();
        if (backingStore == null)
            writer.WriteLine($"this.{codeElement.AccessedProperty?.NamePrefix}{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()} = value;");
        else
            writer.WriteLine($"this.{backingStore.NamePrefix}{backingStore.Name.ToFirstCharacterLowerCase()}.set(\"{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()}\", value);");
    }
    private void WriteGetterBody(CodeMethod codeElement, LanguageWriter writer, CodeClass parentClass)
    {
        var backingStore = parentClass.GetBackingStoreProperty();
        if (backingStore == null)
            writer.WriteLine($"return this.{codeElement.AccessedProperty?.NamePrefix}{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()};");
        else
            if (!(codeElement.AccessedProperty?.Type?.IsNullable ?? true) &&
                !(codeElement.AccessedProperty?.ReadOnly ?? true) &&
                !string.IsNullOrEmpty(codeElement.AccessedProperty?.DefaultValue))
        {
            writer.WriteLines($"let value = this.{backingStore.NamePrefix}{backingStore.Name.ToFirstCharacterLowerCase()}.get<{conventions.GetTypeString(codeElement.AccessedProperty.Type, codeElement)}>(\"{codeElement.AccessedProperty.Name.ToFirstCharacterLowerCase()}\");",
                "if(!value) {");
            writer.IncreaseIndent();
            writer.WriteLines($"value = {codeElement.AccessedProperty.DefaultValue};",
                $"this.{codeElement.AccessedProperty?.NamePrefix}{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()} = value;");
            writer.DecreaseIndent();
            writer.WriteLines("}", "return value;");
        }
        else
            writer.WriteLine($"return this.{backingStore.NamePrefix}{backingStore.Name.ToFirstCharacterLowerCase()}.get(\"{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()}\");");

    }
    private static void WriteDefaultMethodBody(CodeMethod codeElement, LanguageWriter writer)
    {
        var promisePrefix = codeElement.IsAsync ? "Promise.resolve(" : string.Empty;
        var promiseSuffix = codeElement.IsAsync ? ")" : string.Empty;
        writer.WriteLine($"return {promisePrefix}{(codeElement.ReturnType.Name.Equals("string") ? "''" : "{} as any")}{promiseSuffix};");
    }
    private void WriteDeserializerBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer, bool inherits)
    {
        writer.WriteLine($"return {{{(inherits ? $"...super.{codeElement.Name.ToFirstCharacterLowerCase()}()," : string.Empty)}");
        writer.IncreaseIndent();
        foreach (var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom))
        {
            writer.WriteLine($"\"{otherProp.SerializationName ?? otherProp.Name.ToFirstCharacterLowerCase()}\": n => {{ this.{otherProp.Name.ToFirstCharacterLowerCase()} = n.{GetDeserializationMethodName(otherProp.Type)}; }},");
        }
        writer.DecreaseIndent();
        writer.WriteLine("};");
    }
    private void WriteRequestExecutorBody(CodeMethod codeElement, RequestParams requestParams, bool isVoid, string returnType, LanguageWriter writer)
    {
        if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");

        var generatorMethodName = (codeElement.Parent as CodeClass)
                                            .Methods
                                            .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod)
                                            ?.Name
                                            ?.ToFirstCharacterLowerCase();
        writer.WriteLine($"const requestInfo = this.{generatorMethodName}(");
        var requestInfoParameters = new CodeParameter[] { requestParams.requestBody, requestParams.requestConfiguration }
                                        .Select(x => x?.Name).Where(x => x != null);
        if (requestInfoParameters.Any())
        {
            writer.IncreaseIndent();
            writer.WriteLine(requestInfoParameters.Aggregate((x, y) => $"{x}, {y}"));
            writer.DecreaseIndent();
        }
        writer.WriteLine(");");
        var isStream = localConventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase);
        var returnTypeWithoutCollectionSymbol = GetReturnTypeWithoutCollectionSymbol(codeElement, returnType);
        var genericTypeForSendMethod = GetSendRequestMethodName(isVoid, isStream, codeElement.ReturnType.IsCollection, returnTypeWithoutCollectionSymbol);
        var newFactoryParameter = GetTypeFactory(isVoid, isStream, returnTypeWithoutCollectionSymbol);
        var errorMappingVarName = "undefined";
        if (codeElement.ErrorMappings.Any())
        {
            errorMappingVarName = "errorMapping";
            writer.WriteLine($"const {errorMappingVarName}: Record<string, ParsableFactory<Parsable>> = {{");
            writer.IncreaseIndent();
            foreach (var errorMapping in codeElement.ErrorMappings)
            {
                writer.WriteLine($"\"{errorMapping.Key.ToUpperInvariant()}\": {GetFactoryMethodName(errorMapping.Value.Name)},");
            }
            writer.CloseBlock("};");
        }
        writer.WriteLine($"return this.requestAdapter?.{genericTypeForSendMethod}(requestInfo,{newFactoryParameter} responseHandler, {errorMappingVarName}) ?? Promise.reject(new Error('http core is null'));");
    }
    private string GetReturnTypeWithoutCollectionSymbol(CodeMethod codeElement, string fullTypeName)
    {
        if (!codeElement.ReturnType.IsCollection) return fullTypeName;
        var clone = codeElement.ReturnType.Clone() as CodeTypeBase;
        clone.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.None;
        return conventions.GetTypeString(clone, codeElement);
    }
    private const string RequestInfoVarName = "requestInfo";
    private void WriteRequestGeneratorBody(CodeMethod codeElement, RequestParams requestParams, CodeClass currentClass, LanguageWriter writer)
    {
        if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");

        var urlTemplateParamsProperty = currentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
        var urlTemplateProperty = currentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate);
        var requestAdapterProperty = currentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
        writer.WriteLines($"const {RequestInfoVarName} = new RequestInformation();",
                            $"{RequestInfoVarName}.urlTemplate = {GetPropertyCall(urlTemplateProperty, "''")};",
                            $"{RequestInfoVarName}.pathParameters = {GetPropertyCall(urlTemplateParamsProperty, "''")};",
                            $"{RequestInfoVarName}.httpMethod = HttpMethod.{codeElement.HttpMethod.ToString().ToUpperInvariant()};");
        if(codeElement.AcceptedResponseTypes.Any())
            writer.WriteLine($"{RequestInfoVarName}.headers[\"Accept\"] = \"{string.Join(", ", codeElement.AcceptedResponseTypes)}\";");
        if(requestParams.requestConfiguration != null) {
            writer.WriteLine($"if ({requestParams.requestConfiguration.Name}) {{");
            writer.IncreaseIndent();
            var headers = requestParams.Headers;
            if (headers != null)
                writer.WriteLine($"{RequestInfoVarName}.addRequestHeaders({requestParams.requestConfiguration.Name}.{headers.Name});");
            var queryString = requestParams.QueryParameters;
            if (queryString != null)
                writer.WriteLines($"{RequestInfoVarName}.setQueryStringParametersFromRawObject({requestParams.requestConfiguration.Name}.{queryString.Name});");
            var options = requestParams.Options;
            if (options != null)
                writer.WriteLine($"{RequestInfoVarName}.addRequestOptions({requestParams.requestConfiguration.Name}.{options.Name});");
            writer.CloseBlock();
        }
        if (requestParams.requestBody != null)
        {
            ComposeContentInRequestGeneratorBody(requestParams.requestBody, requestAdapterProperty, codeElement.RequestBodyContentType, writer);
        }

        writer.WriteLine($"return {RequestInfoVarName};");
    }

    private void ComposeContentInRequestGeneratorBody(CodeParameter requestBody, CodeProperty requestAdapterProperty, string contentType, LanguageWriter writer)
    {
        if (requestBody.Type.Name.Equals(localConventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
            writer.WriteLine($"{RequestInfoVarName}.setStreamContent({requestBody.Name});");
        else
        {
            var spreadOperator = requestBody.Type.AllTypes.First().IsCollection ? "..." : string.Empty;
            var setMethodName = "";
            var body = "";
            if (IsCodeClassOrInterface(requestBody.Type))
            {
                setMethodName = "setContentFromParsable";
                writer.WriteLine($"const parsableBody = new {requestBody.Type.Name}{ModelClassSuffix}(body)");
                body = "parsableBody";
            }
            else
            {
                setMethodName = "setContentFromScalar";
                body = $"{spreadOperator}{requestBody.Name}";
            }
            writer.WriteLine($"{RequestInfoVarName}.{setMethodName}(this.{requestAdapterProperty.Name.ToFirstCharacterLowerCase()}, \"{contentType}\", {body});");
        }
    }
    private static string GetPropertyCall(CodeProperty property, string defaultValue) => property == null ? defaultValue : $"this.{property.Name}";
    private void WriteSerializerBody(bool inherits, CodeClass parentClass, LanguageWriter writer)
    {
        var additionalDataProperty = parentClass.GetPropertyOfKind(CodePropertyKind.AdditionalData);
        if (inherits)
            writer.WriteLine("super.serialize(writer);");
        foreach (var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom))
        {
            WritePropertySerializer(otherProp, writer);
        }
        if (additionalDataProperty != null)
            writer.WriteLine($"writer.writeAdditionalData(this.{additionalDataProperty.Name.ToFirstCharacterLowerCase()});");
    }

    private void WritePropertySerializer(CodeProperty codeProperty, LanguageWriter writer)
    {
        var isCollectionOfEnum = codeProperty.Type is CodeType cType && cType.IsCollection && cType.TypeDefinition is CodeEnum;
        var spreadOperator = isCollectionOfEnum ? "..." : string.Empty;
        var codePropertyName = codeProperty.Name.ToFirstCharacterLowerCase();
        var undefinedPrefix = isCollectionOfEnum ? $"this.{codePropertyName} && " : string.Empty;
        var isCollection = IsCodePropertyCollection(codeProperty);
        var str = "";

        if (isCollection && !isCollectionOfEnum)
        {
            writer.Write($"if(this.{codePropertyName} && this.{codePropertyName}.length != 0){{");
            str = ConvertPropertyValueToInstanceArray(codePropertyName, codeProperty.Type, writer);
        }
        else
        {
            writer.WriteLine($"if(this.{codePropertyName}){{");
            var propertyType = localConventions.TranslateType(codeProperty.Type);
            str = IsPredefinedType(codeProperty.Type) || !IsCodeClassOrInterface(codeProperty.Type) ? $"{spreadOperator}this.{codePropertyName}" : $"new {propertyType}{ModelClassSuffix}(this.{codePropertyName})";
        }

        writer.IncreaseIndent();
        writer.WriteLine($"{undefinedPrefix}writer.{GetSerializationMethodName(codeProperty.Type)}(\"{codeProperty.SerializationName ?? codePropertyName}\", {str});");
        writer.DecreaseIndent();
        writer.WriteLine("}");
    }

    private static bool IsCodeClassOrInterface(CodeTypeBase propType)
    {
        return propType is CodeType currentType && (currentType.TypeDefinition is CodeClass || currentType.TypeDefinition is CodeInterface);
    }

    private string ConvertPropertyValueToInstanceArray(string propertyName, CodeTypeBase propType, LanguageWriter writer)
    {
        var propertyType = localConventions.TranslateType(propType);
        if (IsCodeClassOrInterface(propType))
        {
            propertyType = propertyType + ModelClassSuffix;
        }

        var arrayName = $"{propertyName}ArrValue".ToFirstCharacterLowerCase();

        writer.WriteLine($"const {arrayName}: {propertyType}[] = []; this.{propertyName}?.forEach(element => {{{arrayName}.push(element instanceof {propertyType}? element : new {propertyType}(element));}});");
        return arrayName;
    }

    private void WriteMethodDocumentation(CodeMethod code, LanguageWriter writer, bool isVoid)
    {
        var isDescriptionPresent = !string.IsNullOrEmpty(code.Description);
        var parametersWithDescription = code.Parameters.Where(x => !string.IsNullOrEmpty(code.Description));
        if (isDescriptionPresent || parametersWithDescription.Any())
        {
            writer.WriteLine(localConventions.DocCommentStart);
            if (isDescriptionPresent)
                writer.WriteLine($"{localConventions.DocCommentPrefix}{TypeScriptConventionService.RemoveInvalidDescriptionCharacters(code.Description)}");
            foreach (var paramWithDescription in parametersWithDescription.OrderBy(x => x.Name))
                writer.WriteLine($"{localConventions.DocCommentPrefix}@param {paramWithDescription.Name} {TypeScriptConventionService.RemoveInvalidDescriptionCharacters(paramWithDescription.Description)}");

            if (!isVoid)
                if (code.IsAsync)
                    writer.WriteLine($"{localConventions.DocCommentPrefix}@returns a Promise of {code.ReturnType.Name.ToFirstCharacterUpperCase()}");
                else
                    writer.WriteLine($"{localConventions.DocCommentPrefix}@returns a {code.ReturnType.Name}");
            writer.WriteLine(localConventions.DocCommentEnd);
        }
    }
    private static readonly CodeParameterOrderComparer parameterOrderComparer = new();
    private void WriteMethodPrototype(CodeMethod code, LanguageWriter writer, string returnType, bool isVoid)
    {
        WriteMethodPrototypeInternal(code, writer, returnType, isVoid, localConventions, false);
    }
    internal static void WriteMethodPrototypeInternal(CodeMethod code, LanguageWriter writer, string returnType, bool isVoid, TypeScriptConventionService pConventions, bool isFunction)
    {
        var accessModifier = isFunction ? string.Empty : pConventions.GetAccessModifier(code.Access);
        var isConstructor = code.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor);
        var methodName = (code.Kind switch
        {
            _ when code.IsAccessor => code.AccessedProperty?.Name,
            _ when isConstructor => "constructor",
            _ => code.Name,
        })?.ToFirstCharacterLowerCase();
        var asyncPrefix = code.IsAsync && code.Kind != CodeMethodKind.RequestExecutor ? " async " : string.Empty;
        var staticPrefix = code.IsStatic && !isFunction ? "static " : string.Empty;
        var functionPrefix = isFunction ? "export function " : " ";
        var parameters = string.Join(", ", code.Parameters.OrderBy(x => x, parameterOrderComparer).Select(p => pConventions.GetParameterSignature(p, code)).ToList());
        var asyncReturnTypePrefix = code.IsAsync ? "Promise<" : string.Empty;
        var asyncReturnTypeSuffix = code.IsAsync ? ">" : string.Empty;
        var nullableSuffix = code.ReturnType.IsNullable && !isVoid ? " | undefined" : string.Empty;
        var accessorPrefix = code.Kind switch
        {
            CodeMethodKind.Getter => "get ",
            CodeMethodKind.Setter => "set ",
            _ => string.Empty
        };
        var shouldHaveTypeSuffix = !code.IsAccessor && !isConstructor;
        var returnTypeSuffix = shouldHaveTypeSuffix ? $" : {asyncReturnTypePrefix}{returnType}{nullableSuffix}{asyncReturnTypeSuffix}" : string.Empty;
        writer.WriteLine($"{accessModifier}{functionPrefix}{accessorPrefix}{staticPrefix}{methodName}{asyncPrefix}({parameters}){returnTypeSuffix} {{");
    }
    private string GetDeserializationMethodName(CodeTypeBase propType)
    {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyType = localConventions.TranslateType(propType);
        if (propType is CodeType currentType)
        {
            if (currentType.TypeDefinition is CodeEnum currentEnum)
                return $"getEnumValue{(currentEnum.Flags || isCollection ? "s" : string.Empty)}<{currentEnum.Name.ToFirstCharacterUpperCase()}>({propertyType.ToFirstCharacterUpperCase()})";
            else if (isCollection)
                if (currentType.TypeDefinition == null)
                    return $"getCollectionOfPrimitiveValues<{propertyType.ToFirstCharacterLowerCase()}>()";
                else
                    return $"getCollectionOfObjectValues<{propertyType.ToFirstCharacterUpperCase()}{ModelClassSuffix}>({GetFactoryMethodName(propertyType)})";
        }
        return propertyType switch
        {
            "string" or "boolean" or "number" or "Guid" or "Date" or "DateOnly" or "TimeOnly" or "Duration" => $"get{propertyType.ToFirstCharacterUpperCase()}Value()",
            _ => $"getObjectValue<{propertyType.ToFirstCharacterUpperCase()}{ModelClassSuffix}>({GetFactoryMethodName(propertyType)})",
        };
    }

    private bool IsPredefinedType(CodeTypeBase propType)
    {
        var propertyType = localConventions.TranslateType(propType);
        return propertyType switch
        {
            "string" or "boolean" or "number" or "Guid" or "Date" or "DateOnly" or "TimeOnly" or "Duration" => true,
            _ => false,
        };

    }

    private static string GetFactoryMethodName(string targetClassName) =>
        $"create{(targetClassName.EndsWith(ModelClassSuffix) ? targetClassName.Split(ModelClassSuffix)[0] : targetClassName).ToFirstCharacterUpperCase()}FromDiscriminatorValue";

    private string GetSerializationMethodName(CodeTypeBase propType)
    {
        var propertyType = localConventions.TranslateType(propType);
        if (propType is CodeType currentType)
        {
            var result = GetSerializationMethodNameForCodeType(currentType, propertyType);
            if (!String.IsNullOrWhiteSpace(result))
            {
                return result;
            }
        }
        return propertyType switch
        {
            "string" or "boolean" or "number" or "Guid" or "Date" or "DateOnly" or "TimeOnly" or "Duration" => $"write{propertyType.ToFirstCharacterUpperCase()}Value",
            _ => $"writeObjectValue<{propertyType.ToFirstCharacterUpperCase()}{ModelClassSuffix}>",
        };
    }

    private static string GetSerializationMethodNameForCodeType(CodeType propType, string propertyType)
    {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        if (propType.TypeDefinition is CodeEnum currentEnum)
            return $"writeEnumValue<{currentEnum.Name.ToFirstCharacterUpperCase()}>";
        else if (isCollection)
        {
            if (propType.TypeDefinition == null)
                return $"writeCollectionOfPrimitiveValues<{propertyType.ToFirstCharacterLowerCase()}>";
            else
                return $"writeCollectionOfObjectValues<{propertyType.ToFirstCharacterUpperCase()}{(IsCodeClassOrInterface(propType) ? ModelClassSuffix : String.Empty)}>";
        }
        return null;
    }
    private string GetTypeFactory(bool isVoid, bool isStream, string returnType)
    {
        if (isVoid) return string.Empty;
        else if (isStream || conventions.IsPrimitiveType(returnType)) return $" \"{returnType}\",";
        else return $" {GetFactoryMethodName(returnType)},";
    }
    private string GetSendRequestMethodName(bool isVoid, bool isStream, bool isCollection, string returnType)
    {
        if (isVoid) return "sendNoResponseContentAsync";
        else if (isCollection)
        {
            if (conventions.IsPrimitiveType(returnType)) return $"sendCollectionOfPrimitiveAsync<{returnType}>";
            else return $"sendCollectionAsync<{returnType}>";
        }
        else if (isStream || conventions.IsPrimitiveType(returnType)) return $"sendPrimitiveAsync<{returnType}>";
        else return $"sendAsync<{returnType}>";
    }
}
