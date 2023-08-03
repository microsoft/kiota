using System;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.TypeScript;
public class CodeMethodWriter : BaseElementWriter<CodeMethod, TypeScriptConventionService>
{
    public CodeMethodWriter(TypeScriptConventionService conventionService, bool usesBackingStore) : base(conventionService)
    {
        _usesBackingStore = usesBackingStore;
    }
    private TypeScriptConventionService? localConventions;
    private readonly bool _usesBackingStore;

    public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        if (codeElement.ReturnType == null) throw new InvalidOperationException($"{nameof(codeElement.ReturnType)} should not be null");
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.Parent is CodeFunction) return;
        if (codeElement.Parent is not CodeClass parentClass) throw new InvalidOperationException("the parent of a method should be a class");

        localConventions = new TypeScriptConventionService(writer); //because we allow inline type definitions for methods parameters
        var returnType = localConventions.GetTypeString(codeElement.ReturnType, codeElement);
        var isVoid = "void".EqualsIgnoreCase(returnType);
        WriteMethodDocumentation(codeElement, writer, isVoid);
        WriteMethodPrototype(codeElement, writer, returnType, isVoid);
        writer.IncreaseIndent();
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
                throw new InvalidOperationException("Deserializers are implemented as functions in TypeScript");
            case CodeMethodKind.Serializer:
                throw new InvalidOperationException("Serializers are implemented as functions in TypeScript");
            case CodeMethodKind.RequestGenerator:
                WriteRequestGeneratorBody(codeElement, requestParams, parentClass, writer);
                break;
            case CodeMethodKind.RequestExecutor:
                WriteRequestExecutorBody(codeElement, parentClass, requestParams, isVoid, returnType, writer);
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
        var parameter = codeElement.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.QueryParametersMapperParameter)) ?? throw new InvalidOperationException("QueryParametersMapper should have a parameter of type QueryParametersMapper");
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

        var isRequestExecutor = codeElement.IsOfKind(CodeMethodKind.RequestExecutor);

        foreach (var parameter in codeElement.Parameters
                                        .Where(x => !x.Optional && !x.IsOfKind(CodeParameterKind.RequestAdapter, CodeParameterKind.PathParameters) &&
                                                !(isRequestExecutor && x.IsOfKind(CodeParameterKind.RequestBody)))
                                        .OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var parameterName = parameter.Name.ToFirstCharacterLowerCase();
            writer.WriteLine($"if(!{parameterName}) throw new Error(\"{parameterName} cannot be undefined\");");
        }
    }
    private void WriteIndexerBody(CodeMethod codeElement, CodeClass parentClass, string returnType, LanguageWriter writer)
    {
        if (parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProperty &&
            localConventions != null &&
            codeElement.OriginalIndexer != null)
        {
            localConventions.AddParametersAssignment(writer, pathParametersProperty.Type, $"this.{pathParametersProperty.Name}",
                parameters: (codeElement.OriginalIndexer.IndexType, codeElement.OriginalIndexer.SerializationName, codeElement.OriginalIndexer.IndexParameterName.ToFirstCharacterLowerCase()));
        }
        conventions.AddRequestBuilderBody(parentClass, returnType, writer, conventions.TempDictionaryVarName);
    }
    private void WriteRequestBuilderWithParametersBody(CodeMethod codeElement, CodeClass parentClass, string returnType, LanguageWriter writer)
    {
        var codePathParameters = codeElement.Parameters
                                                    .Where(x => x.IsOfKind(CodeParameterKind.Path));
        conventions.AddRequestBuilderBody(parentClass, returnType, writer, pathParameters: codePathParameters);
    }
    private static void WriteApiConstructorBody(CodeClass parentClass, CodeMethod method, LanguageWriter writer)
    {
        var pathParametersProperty = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
        var backingStoreParameter = method.Parameters.FirstOrDefault(static x => x.IsOfKind(CodeParameterKind.BackingStore));
        WriteSerializationRegistration(method.SerializerModules, writer, "registerDefaultSerializer");
        WriteSerializationRegistration(method.DeserializerModules, writer, "registerDefaultDeserializer");
        if (parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter)?.Name.ToFirstCharacterLowerCase() is not string requestAdapterPropertyName) return;
        if (!string.IsNullOrEmpty(method.BaseUrl))
        {
            writer.StartBlock($"if ({requestAdapterPropertyName}.baseUrl === undefined || {requestAdapterPropertyName}.baseUrl === \"\") {{");
            writer.WriteLine($"{requestAdapterPropertyName}.baseUrl = \"{method.BaseUrl}\";");
            writer.CloseBlock();
            if (pathParametersProperty != null)
                writer.WriteLine($"this.{pathParametersProperty.Name.ToFirstCharacterLowerCase()}[\"baseurl\"] = {requestAdapterPropertyName}.baseUrl;");
        }
        if (backingStoreParameter != null)
            writer.WriteLine($"this.{requestAdapterPropertyName}.enableBackingStore({backingStoreParameter.Name});");
    }
    private static void WriteSerializationRegistration(HashSet<string> serializationModules, LanguageWriter writer, string methodName)
    {
        if (serializationModules != null)
            foreach (var module in serializationModules)
                writer.WriteLine($"{methodName}({module});");
    }
    private CodePropertyKind[]? _DirectAccessProperties;
    private CodePropertyKind[] DirectAccessProperties
    {
        get
        {
            if (_DirectAccessProperties == null)
            {
                var directAccessProperties = new List<CodePropertyKind> {
                CodePropertyKind.BackingStore,
                CodePropertyKind.RequestBuilder,
                CodePropertyKind.UrlTemplate,
                CodePropertyKind.PathParameters
            };
                if (!_usesBackingStore)
                {
                    directAccessProperties.Add(CodePropertyKind.AdditionalData);
                }
                _DirectAccessProperties = directAccessProperties.ToArray();
            }
            return _DirectAccessProperties;
        }
    }
    private CodePropertyKind[]? _SetterAccessProperties;
    private CodePropertyKind[] SetterAccessProperties
    {
        get
        {
            _SetterAccessProperties ??= new[] {
                    CodePropertyKind.AdditionalData, //additional data and custom properties need to use the accessors in case of backing store use
                    CodePropertyKind.Custom
                }.Except(DirectAccessProperties)
                .ToArray();
            return _SetterAccessProperties;
        }
    }
    private void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer, bool inherits)
    {
        if (inherits || parentClass.IsErrorDefinition)
            if (parentClass.IsOfKind(CodeClassKind.RequestBuilder) &&
                    currentMethod.Parameters.OfKind(CodeParameterKind.RequestAdapter) is CodeParameter requestAdapterParameter &&
                    parentClass.Properties.FirstOrDefaultOfKind(CodePropertyKind.UrlTemplate) is CodeProperty urlTemplateProperty &&
                    !string.IsNullOrEmpty(urlTemplateProperty.DefaultValue))
            {
                var pathParametersValue = "{}";
                if (currentMethod.Parameters.OfKind(CodeParameterKind.PathParameters) is CodeParameter pathParametersParameter)
                    pathParametersValue = pathParametersParameter.Name.ToFirstCharacterLowerCase();
                writer.WriteLine($"super({pathParametersValue}, {requestAdapterParameter.Name.ToFirstCharacterLowerCase()}, {urlTemplateProperty.DefaultValue});");
            }
            else
                writer.WriteLine("super();");

        foreach (var propWithDefault in parentClass.GetPropertiesOfKind(DirectAccessProperties)
                                        .Where(static x => !string.IsNullOrEmpty(x.DefaultValue) && !x.IsOfKind(CodePropertyKind.UrlTemplate))
                                        .OrderByDescending(static x => x.Kind)
                                        .ThenBy(static x => x.Name))
        {
            writer.WriteLine($"this.{propWithDefault.NamePrefix}{propWithDefault.Name.ToFirstCharacterLowerCase()} = {propWithDefault.DefaultValue};");
        }

        foreach (var propWithDefault in parentClass.GetPropertiesOfKind(SetterAccessProperties)
                                        .Where(static x => !string.IsNullOrEmpty(x.DefaultValue) && !x.IsOfKind(CodePropertyKind.UrlTemplate))
                                        // do not apply the default value if the type is composed as the default value may not necessarily which type to use
                                        .Where(static x => x.Type is not CodeType propType || propType.TypeDefinition is not CodeClass propertyClass || propertyClass.OriginalComposedType is null)
                                        .OrderByDescending(static x => x.Kind)
                                        .ThenBy(static x => x.Name))
        {
            var defaultValue = propWithDefault.DefaultValue;
            if (propWithDefault.Type is CodeType propertyType && propertyType.TypeDefinition is CodeEnum enumDefinition)
            {
                defaultValue = $"{enumDefinition.Name.ToFirstCharacterUpperCase()}.{defaultValue.Trim('"').CleanupSymbolName().ToFirstCharacterUpperCase()}";
            }
            writer.WriteLine($"this.{propWithDefault.Name.ToFirstCharacterLowerCase()} = {defaultValue};");
        }
        if (parentClass.IsOfKind(CodeClassKind.RequestBuilder) &&
            currentMethod.IsOfKind(CodeMethodKind.Constructor) &&
                currentMethod.Parameters.FirstOrDefault(static x => x.IsOfKind(CodeParameterKind.PathParameters)) is CodeParameter pathParametersParam &&
                parentClass.Properties.FirstOrDefaultOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProperty)
        {
            localConventions?.AddParametersAssignment(writer,
                                                pathParametersParam.Type.AllTypes.OfType<CodeType>().First(),
                                                pathParametersParam.Name.ToFirstCharacterLowerCase(),
                                                $"this.{pathParametersProperty.Name.ToFirstCharacterLowerCase()}",
                                                currentMethod.Parameters
                                                            .Where(static x => x.IsOfKind(CodeParameterKind.Path))
                                                            .Select(x => (x.Type, string.IsNullOrEmpty(x.SerializationName) ? x.Name : x.SerializationName, x.Name.ToFirstCharacterLowerCase()))
                                                            .ToArray());
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
        writer.WriteLine($"return {promisePrefix}{(codeElement.ReturnType.Name.Equals("string", StringComparison.OrdinalIgnoreCase) ? "''" : "{} as any")}{promiseSuffix};");
    }
    private void WriteRequestExecutorBody(CodeMethod codeElement, CodeClass parentClass, RequestParams requestParams, bool isVoid, string returnType, LanguageWriter writer)
    {
        if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");


        var generatorMethodName = parentClass
                                            .Methods
                                            .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod)
                                            ?.Name
                                            ?.ToFirstCharacterLowerCase();
        writer.WriteLine($"const requestInfo = this.{generatorMethodName}(");
        var requestInfoParameters = new[] { requestParams.requestBody, requestParams.requestConfiguration }
                                        .Select(x => x?.Name).Where(x => x != null)
                                        .ToArray();
        if (requestInfoParameters.Any() && requestInfoParameters.Aggregate((x, y) => $"{x}, {y}") is string requestInfoParametersString)
        {
            writer.IncreaseIndent();
            writer.WriteLine(requestInfoParametersString);
            writer.DecreaseIndent();
        }
        writer.WriteLine(");");
        var isStream = conventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase);
        var returnTypeWithoutCollectionSymbol = GetReturnTypeWithoutCollectionSymbol(codeElement, returnType);
        var genericTypeForSendMethod = GetSendRequestMethodName(isVoid, isStream, codeElement.ReturnType.IsCollection, returnTypeWithoutCollectionSymbol);
        var newFactoryParameter = GetTypeFactory(isVoid, isStream, codeElement, writer);
        var errorMappingVarName = "undefined";
        if (codeElement.ErrorMappings.Any())
        {
            errorMappingVarName = "errorMapping";
            writer.WriteLine($"const {errorMappingVarName} = {{");
            writer.IncreaseIndent();
            foreach (var errorMapping in codeElement.ErrorMappings)
            {
                writer.WriteLine($"\"{errorMapping.Key.ToUpperInvariant()}\": {GetFactoryMethodName(errorMapping.Value, codeElement, writer)},");
            }
            writer.CloseBlock("} as Record<string, ParsableFactory<Parsable>>;");
        }
        writer.WriteLine($"return this.requestAdapter.{genericTypeForSendMethod}(requestInfo,{newFactoryParameter} {errorMappingVarName});");
    }

    private string GetTypeFactory(bool isVoid, bool isStream, CodeMethod codeElement, LanguageWriter writer)
    {
        if (isVoid) return string.Empty;
        var typeName = conventions.TranslateType(codeElement.ReturnType);
        if (isStream || conventions.IsPrimitiveType(typeName)) return $" \"{typeName}\",";
        return $" {GetFactoryMethodName(codeElement.ReturnType, codeElement, writer)},";
    }
    private string GetReturnTypeWithoutCollectionSymbol(CodeMethod codeElement, string fullTypeName)
    {
        if (!codeElement.ReturnType.IsCollection) return fullTypeName;
        var clone = (CodeTypeBase)codeElement.ReturnType.Clone();
        clone.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.None;
        return conventions.GetTypeString(clone, codeElement);
    }
    private const string RequestInfoVarName = "requestInfo";
    private void WriteRequestGeneratorBody(CodeMethod codeElement, RequestParams requestParams, CodeClass currentClass, LanguageWriter writer)
    {
        if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");

        writer.WriteLine($"const {RequestInfoVarName} = new RequestInformation();");
        if (currentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty urlTemplateParamsProperty &&
            currentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate) is CodeProperty urlTemplateProperty)
            writer.WriteLines($"{RequestInfoVarName}.urlTemplate = {GetPropertyCall(urlTemplateProperty)};",
                                $"{RequestInfoVarName}.pathParameters = {GetPropertyCall(urlTemplateParamsProperty)};");
        writer.WriteLine($"{RequestInfoVarName}.httpMethod = HttpMethod.{codeElement.HttpMethod.Value.ToString().ToUpperInvariant()};");
        if (codeElement.AcceptedResponseTypes.Any())
            writer.WriteLine($"{RequestInfoVarName}.headers[\"Accept\"] = [\"{string.Join(", ", codeElement.AcceptedResponseTypes)}\"];");
        if (requestParams.requestConfiguration != null)
        {
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
            if (requestParams.requestBody.Type.Name.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
                writer.WriteLine($"{RequestInfoVarName}.setStreamContent({requestParams.requestBody.Name});");
            else if (currentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is CodeProperty requestAdapterProperty)
            {
                ComposeContentInRequestGeneratorBody(requestParams.requestBody, requestAdapterProperty, codeElement.RequestBodyContentType, writer);
            }
        }

        writer.WriteLine($"return {RequestInfoVarName};");
    }

    private void ComposeContentInRequestGeneratorBody(CodeParameter requestBody, CodeProperty requestAdapterProperty, string contentType, LanguageWriter writer)
    {
        if (requestBody.Type.Name.Equals(localConventions?.StreamTypeName, StringComparison.OrdinalIgnoreCase))
        {
            writer.WriteLine($"{RequestInfoVarName}.setStreamContent({requestBody.Name});");
            return;
        }

        var spreadOperator = requestBody.Type.AllTypes.First().IsCollection ? "..." : string.Empty;
        if (requestBody.Type is CodeType currentType && (currentType.TypeDefinition is CodeInterface || currentType.Name.Equals("MultipartBody", StringComparison.OrdinalIgnoreCase)))
        {
            var serializerName = $"serialize{currentType.Name.ToFirstCharacterUpperCase()}";
            writer.WriteLine($"{RequestInfoVarName}.setContentFromParsable(this.{requestAdapterProperty.Name.ToFirstCharacterLowerCase()}, \"{contentType}\", {requestBody.Name}, {serializerName});");
        }
        else
        {
            writer.WriteLine($"{RequestInfoVarName}.setContentFromScalar(this.{requestAdapterProperty.Name.ToFirstCharacterLowerCase()}, \"{contentType}\", {spreadOperator}{requestBody.Name});");
        }
    }
    private static string GetPropertyCall(CodeProperty property) => $"this.{property.Name}";

    private void WriteMethodDocumentation(CodeMethod code, LanguageWriter writer, bool isVoid)
    {
        var returnRemark = (isVoid, code.IsAsync) switch
        {
            (true, _) => string.Empty,
            (false, true) => $"@returns a Promise of {code.ReturnType.Name.ToFirstCharacterUpperCase()}",
            (false, false) => $"@returns a {code.ReturnType.Name}",
        };
        conventions.WriteLongDescription(code,
                                        writer,
                                        code.Parameters
                                            .Where(static x => x.Documentation.DescriptionAvailable)
                                            .OrderBy(static x => x.Name)
                                            .Select(x => $"@param {x.Name} {TypeScriptConventionService.RemoveInvalidDescriptionCharacters(x.Documentation.Description)}")
                                            .Union(new[] { returnRemark }));
    }
    private static readonly BaseCodeParameterOrderComparer parameterOrderComparer = new();
    private void WriteMethodPrototype(CodeMethod code, LanguageWriter writer, string returnType, bool isVoid)
    {
        if (localConventions != null)
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
        var parameters = string.Join(", ", code.Parameters.Order(parameterOrderComparer).Select(p => pConventions.GetParameterSignature(p, code)));
        var asyncReturnTypePrefix = code.IsAsync ? "Promise<" : string.Empty;
        var asyncReturnTypeSuffix = code.IsAsync ? ">" : string.Empty;
        var nullableSuffix = code.ReturnType.IsNullable && !isVoid ? " | undefined" : string.Empty;
        var accessorPrefix = code.Kind switch
        {
            CodeMethodKind.Getter => "get ",
            CodeMethodKind.Setter => "set ",
            _ => string.Empty
        };
        var shouldHaveTypeSuffix = !code.IsAccessor && !isConstructor && !string.IsNullOrEmpty(returnType);
        var returnTypeSuffix = shouldHaveTypeSuffix ? $" : {asyncReturnTypePrefix}{returnType}{nullableSuffix}{asyncReturnTypeSuffix}" : string.Empty;
        writer.WriteLine($"{accessModifier}{functionPrefix}{accessorPrefix}{staticPrefix}{methodName}{asyncPrefix}({parameters}){returnTypeSuffix} {{");
    }
    private string GetFactoryMethodName(CodeTypeBase targetClassType, CodeMethod currentElement, LanguageWriter writer)
    {
        if (localConventions == null) throw new InvalidOperationException("localConventions is null");
        var returnType = localConventions.GetTypeString(targetClassType, currentElement, false, writer);
        var targetClassName = localConventions.TranslateType(targetClassType);
        var resultName = $"create{targetClassName.ToFirstCharacterUpperCase()}FromDiscriminatorValue";
        if (targetClassName.Equals(returnType, StringComparison.OrdinalIgnoreCase))
            return resultName;
        if (targetClassType is CodeType currentType &&
            currentType.TypeDefinition is CodeClass definitionClass &&
            definitionClass.GetImmediateParentOfType<CodeNamespace>() is CodeNamespace parentNamespace &&
            parentNamespace.FindChildByName<CodeFunction>(resultName) is CodeFunction factoryMethod)
        {
            var methodName = localConventions.GetTypeString(new CodeType
            {
                Name = resultName,
                TypeDefinition = factoryMethod
            }, currentElement, false, writer);
            return methodName.ToFirstCharacterUpperCase();// static function is aliased
        }
        throw new InvalidOperationException($"Unable to find factory method for {targetClassName}");
    }

    private string GetSendRequestMethodName(bool isVoid, bool isStream, bool isCollection, string returnType)
    {
        if (isVoid) return "sendNoResponseContentAsync";
        if (isCollection)
        {
            if (conventions.IsPrimitiveType(returnType)) return $"sendCollectionOfPrimitiveAsync<{returnType}>";
            return $"sendCollectionAsync<{returnType}>";
        }

        if (isStream || conventions.IsPrimitiveType(returnType)) return $"sendPrimitiveAsync<{returnType}>";
        return $"sendAsync<{returnType}>";
    }
}
