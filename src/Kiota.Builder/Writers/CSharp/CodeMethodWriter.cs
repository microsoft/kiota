﻿using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.CSharp;
public class CodeMethodWriter : BaseElementWriter<CodeMethod, CSharpConventionService>
{
    public CodeMethodWriter(CSharpConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
    {
        if (codeElement == null) throw new ArgumentNullException(nameof(codeElement));
        if (codeElement.ReturnType == null) throw new InvalidOperationException($"{nameof(codeElement.ReturnType)} should not be null");
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        if (!(codeElement.Parent is CodeClass)) throw new InvalidOperationException("the parent of a method should be a class");

        var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement);
        var parentClass = codeElement.Parent as CodeClass;
        var inherits = parentClass.StartBlock.Inherits != null && !parentClass.IsErrorDefinition;
        var isVoid = conventions.VoidTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase);
        WriteMethodDocumentation(codeElement, writer);
        WriteMethodPrototype(codeElement, writer, returnType, inherits, isVoid);
        writer.IncreaseIndent();
        foreach (var parameter in codeElement.Parameters.Where(x => !x.Optional).OrderBy(x => x.Name))
        {
            var parameterName = parameter.Name.ToFirstCharacterLowerCase();
            if (nameof(String).Equals(parameter.Type.Name, StringComparison.OrdinalIgnoreCase))
                writer.WriteLine($"if(string.IsNullOrEmpty({parameterName})) throw new ArgumentNullException(nameof({parameterName}));");
            else
                writer.WriteLine($"_ = {parameterName} ?? throw new ArgumentNullException(nameof({parameterName}));");
        }
        HandleMethodKind(codeElement, writer, inherits, parentClass, isVoid);
        writer.CloseBlock();
    }

    protected virtual void HandleMethodKind(CodeMethod codeElement, LanguageWriter writer, bool inherits, CodeClass parentClass, bool isVoid)
    {
        var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement);
        var returnTypeWithoutCollectionInformation = conventions.GetTypeString(codeElement.ReturnType, codeElement, false);
        var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
        var requestConfig = codeElement.Parameters.OfKind(CodeParameterKind.RequestConfiguration);
        var requestParams = new RequestParams(requestBodyParam, requestConfig);
        switch (codeElement.Kind)
        {
            case CodeMethodKind.Serializer:
                WriteSerializerBody(inherits, codeElement, parentClass, writer);
                break;
            case CodeMethodKind.RequestGenerator:
                WriteRequestGeneratorBody(codeElement, requestParams, parentClass, writer);
                break;
            case CodeMethodKind.RequestExecutor:
                WriteRequestExecutorBody(codeElement, requestParams, isVoid, returnType, returnTypeWithoutCollectionInformation, writer);
                break;
            case CodeMethodKind.Deserializer:
                WriteDeserializerBody(inherits, codeElement, parentClass, writer);
                break;
            case CodeMethodKind.ClientConstructor:
                WriteConstructorBody(parentClass, codeElement, writer);
                WriteApiConstructorBody(parentClass, codeElement, writer);
                break;
            case CodeMethodKind.Constructor:
            case CodeMethodKind.RawUrlConstructor:
                WriteConstructorBody(parentClass, codeElement, writer);
                break;
            case CodeMethodKind.RequestBuilderWithParameters:
                WriteRequestBuilderBody(parentClass, codeElement, writer);
                break;
            case CodeMethodKind.Getter:
            case CodeMethodKind.Setter:
                throw new InvalidOperationException("getters and setters are automatically added on fields in dotnet");
            case CodeMethodKind.RequestBuilderBackwardCompatibility:
                throw new InvalidOperationException("RequestBuilderBackwardCompatibility is not supported as the request builders are implemented by properties.");
            case CodeMethodKind.CommandBuilder:
                var origParams = codeElement.OriginalMethod?.Parameters ?? codeElement.Parameters;
                requestBodyParam = origParams.OfKind(CodeParameterKind.RequestBody);
                requestParams = new RequestParams(requestBodyParam, null);
                WriteCommandBuilderBody(codeElement, requestParams, isVoid, returnType, writer);
                break;
            case CodeMethodKind.Factory:
                WriteFactoryMethodBody(codeElement, writer);
                break;
            default:
                writer.WriteLine("return null;");
                break;
        }
    }
    private void WriteFactoryMethodBody(CodeMethod codeElement, LanguageWriter writer){
        var parseNodeParameter = codeElement.Parameters.OfKind(CodeParameterKind.ParseNode);
        if(codeElement.ShouldWriteDiscriminatorSwitch && parseNodeParameter != null) {
            writer.WriteLine($"var mappingValueNode = {parseNodeParameter.Name.ToFirstCharacterLowerCase()}.GetChildNode(\"{codeElement.DiscriminatorPropertyName}\");");
            writer.WriteLines($"var mappingValue = mappingValueNode?.GetStringValue();");
            writer.WriteLine("return mappingValue switch {");
            writer.IncreaseIndent();
            foreach(var mappedType in codeElement.DiscriminatorMappings) {
                writer.WriteLine($"\"{mappedType.Key}\" => new {conventions.GetTypeString(mappedType.Value.AllTypes.First() ,codeElement)}(),");
            }
            writer.WriteLine($"_ => new {codeElement.Parent.Name.ToFirstCharacterUpperCase()}(),");
            writer.CloseBlock("};");
        } else 
            writer.WriteLine($"return new {codeElement.Parent.Name.ToFirstCharacterUpperCase()}();");
    }
    private void WriteRequestBuilderBody(CodeClass parentClass, CodeMethod codeElement, LanguageWriter writer)
    {
        var importSymbol = conventions.GetTypeString(codeElement.ReturnType, parentClass);
        conventions.AddRequestBuilderBody(parentClass, importSymbol, writer, prefix: "return ", pathParameters: codeElement.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Path)));
    }
    private static void WriteApiConstructorBody(CodeClass parentClass, CodeMethod method, LanguageWriter writer)
    {
        var requestAdapterProperty = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
        var backingStoreParameter = method.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.BackingStore));
        var requestAdapterPropertyName = requestAdapterProperty.Name.ToFirstCharacterUpperCase();
        WriteSerializationRegistration(method.SerializerModules, writer, "RegisterDefaultSerializer");
        WriteSerializationRegistration(method.DeserializerModules, writer, "RegisterDefaultDeserializer");
        writer.WriteLine($"if (string.IsNullOrEmpty({requestAdapterPropertyName}.BaseUrl)) {{");
        writer.IncreaseIndent();
        writer.WriteLine($"{requestAdapterPropertyName}.BaseUrl = \"{method.BaseUrl}\";");
        writer.CloseBlock();
        if (backingStoreParameter != null)
            writer.WriteLine($"{requestAdapterPropertyName}.EnableBackingStore({backingStoreParameter.Name});");
    }
    private static void WriteSerializationRegistration(HashSet<string> serializationClassNames, LanguageWriter writer, string methodName)
    {
        if (serializationClassNames != null)
            foreach (var serializationClassName in serializationClassNames)
                writer.WriteLine($"ApiClientBuilder.{methodName}<{serializationClassName}>();");
    }
    private void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer)
    {
        foreach (var propWithDefault in parentClass
                                        .Properties
                                        .Where(x => !string.IsNullOrEmpty(x.DefaultValue))
                                        .OrderByDescending(x => x.Kind)
                                        .ThenBy(x => x.Name)) {
            writer.WriteLine($"{propWithDefault.Name.ToFirstCharacterUpperCase()} = {propWithDefault.DefaultValue};");
        }
        if (parentClass.IsOfKind(CodeClassKind.RequestBuilder))
        {
            if (currentMethod.IsOfKind(CodeMethodKind.Constructor))
            {
                var pathParametersParam = currentMethod.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.PathParameters));
                conventions.AddParametersAssignment(writer,
                                                    pathParametersParam.Type,
                                                    pathParametersParam.Name.ToFirstCharacterLowerCase(),
                                                    currentMethod.Parameters
                                                                .Where(x => x.IsOfKind(CodeParameterKind.Path))
                                                                .Select(x => (x.Type, string.IsNullOrEmpty(x.SerializationName) ? x.Name : x.SerializationName, x.Name.ToFirstCharacterLowerCase()))
                                                                .ToArray());
                AssignPropertyFromParameter(parentClass, currentMethod, CodeParameterKind.PathParameters, CodePropertyKind.PathParameters, writer, conventions.TempDictionaryVarName);
            }
            else if (currentMethod.IsOfKind(CodeMethodKind.RawUrlConstructor))
            {
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
    private static void AssignPropertyFromParameter(CodeClass parentClass, CodeMethod currentMethod, CodeParameterKind parameterKind, CodePropertyKind propertyKind, LanguageWriter writer, string variableName = default)
    {
        var property = parentClass.GetPropertyOfKind(propertyKind);
        if (property != null)
        {
            var parameter = currentMethod.Parameters.FirstOrDefault(x => x.IsOfKind(parameterKind));
            if (!string.IsNullOrEmpty(variableName))
                writer.WriteLine($"{property.Name.ToFirstCharacterUpperCase()} = {variableName};");
            else if (parameter != null)
                writer.WriteLine($"{property.Name.ToFirstCharacterUpperCase()} = {parameter.Name};");
        }
    }
    private void WriteDeserializerBody(bool shouldHide, CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        var parentSerializationInfo = shouldHide ? $"(base.{codeElement.Name.ToFirstCharacterUpperCase()}())" : string.Empty;
        writer.WriteLine($"return new Dictionary<string, Action<{conventions.ParseNodeInterfaceName}>>{parentSerializationInfo} {{");
        writer.IncreaseIndent();
        foreach (var otherProp in parentClass
                                        .Properties
                                        .Where(static x => !x.ExistsInBaseType && x.IsOfKind(CodePropertyKind.Custom))
                                        .OrderBy(static x => x.Name))
        {
            writer.WriteLine($"{{\"{otherProp.SerializationName ?? otherProp.Name.ToFirstCharacterLowerCase()}\", n => {{ {otherProp.Name.ToFirstCharacterUpperCase()} = n.{GetDeserializationMethodName(otherProp.Type, codeElement)}; }} }},");
        }
        writer.DecreaseIndent();
        writer.WriteLine("};");
    }
    private string GetDeserializationMethodName(CodeTypeBase propType, CodeMethod method)
    {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyType = conventions.GetTypeString(propType, method, false);
        if (propType is CodeType currentType)
        {
            if (isCollection) {
                var collectionMethod = propType.IsArray ? ".ToArray()" : ".ToList()";
                if (currentType.TypeDefinition == null)
                    return $"GetCollectionOfPrimitiveValues<{propertyType}>(){collectionMethod}";
                else if (currentType.TypeDefinition is CodeEnum enumType)
                    return $"GetCollectionOfEnumValues<{enumType.Name.ToFirstCharacterUpperCase()}>(){collectionMethod}";
                else
                    return $"GetCollectionOfObjectValues<{propertyType}>({propertyType}.CreateFromDiscriminatorValue){collectionMethod}";
            } else if (currentType.TypeDefinition is CodeEnum enumType)
                return $"GetEnumValue<{enumType.Name.ToFirstCharacterUpperCase()}>()";
        }
        return propertyType switch
        {
            "byte[]" => "GetByteArrayValue()",
            _ when conventions.IsPrimitiveType(propertyType) => $"Get{propertyType.TrimEnd(CSharpConventionService.NullableMarker).ToFirstCharacterUpperCase()}Value()",
            _ => $"GetObjectValue<{propertyType.ToFirstCharacterUpperCase()}>({propertyType}.CreateFromDiscriminatorValue)",
        };
    }
    protected void WriteRequestExecutorBody(CodeMethod codeElement, RequestParams requestParams, bool isVoid, string returnType, string returnTypeWithoutCollectionInformation, LanguageWriter writer)
    {
        if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");

        var generatorMethodName = (codeElement.Parent as CodeClass)
                                            .Methods
                                            .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod)
                                            ?.Name;
        var parametersList = new CodeParameter[] { requestParams.requestBody, requestParams.requestConfiguration }
                            .Select(x => x?.Name).Where(x => x != null).Aggregate((x, y) => $"{x}, {y}");
        writer.WriteLine($"var requestInfo = {generatorMethodName}({parametersList});");
        var errorMappingVarName = "default";
        if (codeElement.ErrorMappings.Any())
        {
            errorMappingVarName = "errorMapping";
            writer.WriteLine($"var {errorMappingVarName} = new Dictionary<string, ParsableFactory<IParsable>> {{");
            writer.IncreaseIndent();
            foreach (var errorMapping in codeElement.ErrorMappings)
            {
                writer.WriteLine($"{{\"{errorMapping.Key.ToUpperInvariant()}\", {conventions.GetTypeString(errorMapping.Value, codeElement, false)}.CreateFromDiscriminatorValue}},");
            }
            writer.CloseBlock("};");
        }
        var returnTypeCodeType = codeElement.ReturnType as CodeType; 
        var returnTypeFactory = returnTypeCodeType?.TypeDefinition is CodeClass returnTypeClass
                                ? $", {returnTypeWithoutCollectionInformation}.CreateFromDiscriminatorValue"
                                : null;
        var prefix = (isVoid, codeElement.ReturnType.IsCollection) switch {
            (true, _) => string.Empty,
            (_, true) => "var collectionResult = ",
            (_, _ ) => "return ",
        };
        writer.WriteLine($"{prefix}await RequestAdapter.{GetSendRequestMethodName(isVoid, codeElement.Parent, codeElement.ReturnType)}(requestInfo{returnTypeFactory}, responseHandler, {errorMappingVarName}, cancellationToken);");
        if (codeElement.ReturnType.IsCollection)
            writer.WriteLine("return collectionResult.ToList();");
    }
    private const string RequestInfoVarName = "requestInfo";
    private const string RequestConfigVarName = "requestConfig";
    private void WriteRequestGeneratorBody(CodeMethod codeElement, RequestParams requestParams, CodeClass currentClass, LanguageWriter writer)
    {
        if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");

        var operationName = codeElement.HttpMethod.ToString();
        var urlTemplateParamsProperty = currentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
        var urlTemplateProperty = currentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate);
        var requestAdapterProperty = currentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
        writer.WriteLine($"var {RequestInfoVarName} = new RequestInformation {{");
        writer.IncreaseIndent();
        writer.WriteLines($"HttpMethod = Method.{operationName?.ToUpperInvariant()},",
                        $"UrlTemplate = {GetPropertyCall(urlTemplateProperty, "string.Empty")},",
                        $"PathParameters = {GetPropertyCall(urlTemplateParamsProperty, "string.Empty")},");
        writer.DecreaseIndent();
        writer.WriteLine("};");
        if(codeElement.AcceptedResponseTypes.Any())
            writer.WriteLine($"{RequestInfoVarName}.Headers.Add(\"Accept\", \"{string.Join(", ", codeElement.AcceptedResponseTypes)}\");");
        if (requestParams.requestBody != null)
        {
            var suffix = requestParams.requestBody.Type.IsCollection ? ".ToArray()" : string.Empty;
            if (requestParams.requestBody.Type.Name.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
                writer.WriteLine($"{RequestInfoVarName}.SetStreamContent({requestParams.requestBody.Name});");
            else if (requestParams.requestBody.Type is CodeType bodyType && bodyType.TypeDefinition is CodeClass)
                writer.WriteLine($"{RequestInfoVarName}.SetContentFromParsable({requestAdapterProperty.Name.ToFirstCharacterUpperCase()}, \"{codeElement.RequestBodyContentType}\", {requestParams.requestBody.Name}{suffix});");
            else
                writer.WriteLine($"{RequestInfoVarName}.SetContentFromScalar({requestAdapterProperty.Name.ToFirstCharacterUpperCase()}, \"{codeElement.RequestBodyContentType}\", {requestParams.requestBody.Name}{suffix});");
        }
        
        if (requestParams.requestConfiguration != null)
        {
            writer.WriteLine($"if ({requestParams.requestConfiguration.Name} != null) {{");
            writer.IncreaseIndent();
            writer.WriteLines($"var {RequestConfigVarName} = new {requestParams.requestConfiguration.Type.Name.ToFirstCharacterUpperCase()}();",
                            $"{requestParams.requestConfiguration.Name}.Invoke({RequestConfigVarName});");
            var queryString = requestParams.QueryParameters;
            var headers = requestParams.Headers;
            var options = requestParams.Options;
            if (queryString != null)
                writer.WriteLine($"{RequestInfoVarName}.AddQueryParameters({RequestConfigVarName}.{queryString.Name.ToFirstCharacterUpperCase()});");
            if (options != null)
                writer.WriteLine($"{RequestInfoVarName}.AddRequestOptions({RequestConfigVarName}.{options.Name.ToFirstCharacterUpperCase()});");
            if (headers != null)
                writer.WriteLine($"{RequestInfoVarName}.AddHeaders({RequestConfigVarName}.{headers.Name.ToFirstCharacterUpperCase()});");
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
        writer.WriteLine($"return {RequestInfoVarName};");
    }
    private static string GetPropertyCall(CodeProperty property, string defaultValue) => property == null ? defaultValue : $"{property.Name.ToFirstCharacterUpperCase()}";
    private void WriteSerializerBody(bool shouldHide, CodeMethod method, CodeClass parentClass, LanguageWriter writer)
    {
        var additionalDataProperty = parentClass.GetPropertyOfKind(CodePropertyKind.AdditionalData);
        if (shouldHide)
            writer.WriteLine("base.Serialize(writer);");
        foreach (var otherProp in parentClass
                                        .Properties
                                        .Where(static x => !x.ExistsInBaseType && x.IsOfKind(CodePropertyKind.Custom))
                                        .OrderBy(static x => x.Name))
        {
            writer.WriteLine($"writer.{GetSerializationMethodName(otherProp.Type, method)}(\"{otherProp.SerializationName ?? otherProp.Name.ToFirstCharacterLowerCase()}\", {otherProp.Name.ToFirstCharacterUpperCase()});");
        }
        if (additionalDataProperty != null)
            writer.WriteLine($"writer.WriteAdditionalData({additionalDataProperty.Name});");
    }

    protected virtual void WriteCommandBuilderBody(CodeMethod codeElement, RequestParams requestParams, bool isVoid, string returnType, LanguageWriter writer)
    {
        throw new InvalidOperationException("CommandBuilder methods are not implemented in this SDK. They're currently only supported in the shell language.");
    }

    protected string GetSendRequestMethodName(bool isVoid, CodeElement currentElement, CodeTypeBase returnType)
    {
        var returnTypeName = conventions.GetTypeString(returnType, currentElement, false);
        var isStream = conventions.StreamTypeName.Equals(returnTypeName, StringComparison.OrdinalIgnoreCase);
        var isEnum = returnType is CodeType codeType && codeType.TypeDefinition is CodeEnum;
        if (isVoid) return "SendNoContentAsync";
        else if (isStream || conventions.IsPrimitiveType(returnTypeName) || isEnum)
            if (returnType.IsCollection)
                return $"SendPrimitiveCollectionAsync<{returnTypeName}>";
            else
                return $"SendPrimitiveAsync<{returnTypeName}>";
        else if (returnType.IsCollection) return $"SendCollectionAsync<{returnTypeName}>";
        else return $"SendAsync<{returnTypeName}>";
    }
    private void WriteMethodDocumentation(CodeMethod code, LanguageWriter writer)
    {
        var isDescriptionPresent = !string.IsNullOrEmpty(code.Description);
        var parametersWithDescription = code.Parameters.Where(x => !string.IsNullOrEmpty(code.Description));
        if (isDescriptionPresent || parametersWithDescription.Any())
        {
            writer.WriteLine($"{conventions.DocCommentPrefix}<summary>");
            if (isDescriptionPresent)
                writer.WriteLine($"{conventions.DocCommentPrefix}{code.Description.CleanupXMLString()}");
            foreach (var paramWithDescription in parametersWithDescription.OrderBy(x => x.Name))
                writer.WriteLine($"{conventions.DocCommentPrefix}<param name=\"{paramWithDescription.Name.ToFirstCharacterLowerCase()}\">{paramWithDescription.Description.CleanupXMLString()}</param>");
            writer.WriteLine($"{conventions.DocCommentPrefix}</summary>");
        }
    }
    private static readonly CodeParameterOrderComparer parameterOrderComparer = new();
    private void WriteMethodPrototype(CodeMethod code, LanguageWriter writer, string returnType, bool inherits, bool isVoid)
    {
        var staticModifier = code.IsStatic ? "static " : string.Empty;
        var hideModifier = inherits && code.IsOfKind(CodeMethodKind.Serializer, CodeMethodKind.Deserializer, CodeMethodKind.Factory) ? "new " : string.Empty;
        var genericTypePrefix = isVoid ? string.Empty : "<";
        var genericTypeSuffix = code.IsAsync && !isVoid ? ">" : string.Empty;
        var isConstructor = code.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor, CodeMethodKind.RawUrlConstructor);
        var asyncPrefix = code.IsAsync ? "async Task" + genericTypePrefix : string.Empty;
        var voidCorrectedTaskReturnType = code.IsAsync && isVoid ? string.Empty : returnType;
        if (code.ReturnType.IsArray && code.IsOfKind(CodeMethodKind.RequestExecutor))
            voidCorrectedTaskReturnType = $"IEnumerable<{voidCorrectedTaskReturnType.StripArraySuffix()}>";
        // TODO: Task type should be moved into the refiner
        var completeReturnType = isConstructor ?
            string.Empty :
            $"{asyncPrefix}{voidCorrectedTaskReturnType}{genericTypeSuffix} ";
        var baseSuffix = string.Empty;
        if (isConstructor && inherits)
            baseSuffix = " : base()";
        var parameters = string.Join(", ", code.Parameters.OrderBy(x => x, parameterOrderComparer).Select(p => conventions.GetParameterSignature(p, code)).ToList());
        var methodName = isConstructor ? code.Parent.Name.ToFirstCharacterUpperCase() : code.Name.ToFirstCharacterUpperCase();
        writer.WriteLine($"{conventions.GetAccessModifier(code.Access)} {staticModifier}{hideModifier}{completeReturnType}{methodName}({parameters}){baseSuffix} {{");
    }
    private string GetSerializationMethodName(CodeTypeBase propType, CodeMethod method)
    {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyType = conventions.GetTypeString(propType, method, false);
        if (propType is CodeType currentType)
        {
            if (isCollection)
                if (currentType.TypeDefinition == null)
                    return $"WriteCollectionOfPrimitiveValues<{propertyType}>";
                else if (currentType.TypeDefinition is CodeEnum enumType)
                    return $"WriteCollectionOfEnumValues<{enumType.Name.ToFirstCharacterUpperCase()}>";
                else
                    return $"WriteCollectionOfObjectValues<{propertyType}>";
            else if (currentType.TypeDefinition is CodeEnum enumType)
                return $"WriteEnumValue<{enumType.Name.ToFirstCharacterUpperCase()}>";

        }
        return propertyType switch
        {
            "byte[]" => "WriteByteArrayValue",
            _ when conventions.IsPrimitiveType(propertyType) => $"Write{propertyType.TrimEnd(CSharpConventionService.NullableMarker).ToFirstCharacterUpperCase()}Value",
            _ => $"WriteObjectValue<{propertyType.ToFirstCharacterUpperCase()}>",
        };
    }
}
