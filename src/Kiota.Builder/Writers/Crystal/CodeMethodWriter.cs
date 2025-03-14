using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.OrderComparers;
using Kiota.Builder.Writers.CSharp;

namespace Kiota.Builder.Writers.Crystal;
public class CodeMethodWriter : BaseElementWriter<CodeMethod, CrystalConventionService>
{
    public CodeMethodWriter(CrystalConventionService conventionService) : base(conventionService)
    {
    }
    
    private string ToSnakeCase(string input)
    {
        return conventions.GetMethodName(input);
    }

    public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        if (codeElement.ReturnType == null) throw new InvalidOperationException($"{nameof(codeElement.ReturnType)} should not be null");
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.Parent is not CodeClass parentClass) throw new InvalidOperationException("the parent of a method should be a class");

        var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement);
        var inherits = parentClass.StartBlock.Inherits != null && !parentClass.IsErrorDefinition;
        var isVoid = conventions.VoidTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase);
        WriteMethodDocumentation(codeElement, writer);
        WriteMethodPrototype(codeElement, parentClass, writer, returnType, inherits, isVoid);
        writer.IncreaseIndent();
        foreach (var parameter in codeElement.Parameters.Where(static x => !x.Optional && !x.IsOfKind(CodeParameterKind.RequestAdapter, CodeParameterKind.PathParameters, CodeParameterKind.RawUrl)).OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var parameterName = parameter.Name.ToFirstCharacterLowerCase();
            if (nameof(String).Equals(parameter.Type.Name, StringComparison.OrdinalIgnoreCase) && parameter.Type.CollectionKind == CodeTypeBase.CodeTypeCollectionKind.None)
                writer.WriteLine($"raise ArgumentError.new(\"{parameterName} cannot be null or empty\") if {parameterName}.nil? || {parameterName}.empty?");
            else
                writer.WriteLine($"raise ArgumentError.new(\"{parameterName} cannot be null\") if {parameterName}.nil?");
        }
        HandleMethodKind(codeElement, writer, inherits, parentClass, isVoid);
        writer.CloseBlock();
    }

    protected virtual void HandleMethodKind(CodeMethod codeElement, LanguageWriter writer, bool doesInherit, CodeClass parentClass, bool isVoid)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(parentClass);
        var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement);
        var returnTypeWithoutCollectionInformation = conventions.GetTypeString(codeElement.ReturnType, codeElement, false);
        var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
        var requestConfig = codeElement.Parameters.OfKind(CodeParameterKind.RequestConfiguration);
        var requestContentType = codeElement.Parameters.OfKind(CodeParameterKind.RequestBodyContentType);
        var requestParams = new RequestParams(requestBodyParam, requestConfig, requestContentType);
        switch (codeElement.Kind)
        {
            case CodeMethodKind.Serializer:
                WriteSerializerBody(doesInherit, codeElement, parentClass, writer);
                break;
            case CodeMethodKind.RequestGenerator:
                WriteRequestGeneratorBody(codeElement, requestParams, parentClass, writer);
                break;
            case CodeMethodKind.RequestExecutor:
                WriteRequestExecutorBody(codeElement, requestParams, parentClass, isVoid, returnTypeWithoutCollectionInformation, writer);
                break;
            case CodeMethodKind.Deserializer:
                WriteDeserializerBody(doesInherit, codeElement, parentClass, writer);
                break;
            case CodeMethodKind.ClientConstructor:
                WriteConstructorBody(parentClass, codeElement, writer);
                WriteApiConstructorBody(parentClass, codeElement, writer);
                break;
            case CodeMethodKind.RawUrlBuilder:
                WriteRawUrlBuilderBody(parentClass, codeElement, writer);
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
                throw new InvalidOperationException("getters and setters are automatically added on fields in Crystal");
            case CodeMethodKind.RequestBuilderBackwardCompatibility:
                throw new InvalidOperationException("RequestBuilderBackwardCompatibility is not supported as the request builders are implemented by properties.");
            case CodeMethodKind.ErrorMessageOverride:
                throw new InvalidOperationException("ErrorMessageOverride is not supported as the error message is implemented by a property.");
            case CodeMethodKind.CommandBuilder:
                var origParams = codeElement.OriginalMethod?.Parameters ?? codeElement.Parameters;
                requestBodyParam = origParams.OfKind(CodeParameterKind.RequestBody);
                requestConfig = origParams.OfKind(CodeParameterKind.RequestConfiguration);
                requestContentType = origParams.OfKind(CodeParameterKind.RequestBodyContentType);
                requestParams = new RequestParams(requestBodyParam, requestConfig, requestContentType);
                WriteCommandBuilderBody(codeElement, parentClass, requestParams, isVoid, returnType, writer);
                break;
            case CodeMethodKind.Factory:
                WriteFactoryMethodBody(codeElement, parentClass, writer);
                break;
            case CodeMethodKind.ComposedTypeMarker:
                throw new InvalidOperationException("ComposedTypeMarker is not required as interface is explicitly implemented.");
            default:
                writer.WriteLine("return nil");
                break;
        }
    }
    private void WriteRawUrlBuilderBody(CodeClass parentClass, CodeMethod codeElement, LanguageWriter writer)
    {
        var rawUrlParameter = codeElement.Parameters.OfKind(CodeParameterKind.RawUrl) ?? throw new InvalidOperationException("RawUrlBuilder method should have a RawUrl parameter");
        var requestAdapterProperty = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) ?? throw new InvalidOperationException("RawUrlBuilder method should have a RequestAdapter property");

        var fullName = parentClass.GetFullName();
        writer.WriteLine($"return {fullName}.new({rawUrlParameter.Name.ToFirstCharacterLowerCase()}, {requestAdapterProperty.Name.ToFirstCharacterLowerCase()})");
    }
    private static readonly CodePropertyTypeComparer CodePropertyTypeForwardComparer = new();
    private static readonly CodePropertyTypeComparer CodePropertyTypeBackwardComparer = new(true);
    private void WriteFactoryMethodBodyForInheritedModel(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        writer.WriteLine($"case {DiscriminatorMappingVarName}");
        writer.StartBlock();
        foreach (var mappedType in parentClass.DiscriminatorInformation.DiscriminatorMappings)
        {
            writer.WriteLine($"when \"{mappedType.Key}\" then return {conventions.GetTypeString(mappedType.Value.AllTypes.First(), codeElement)}.new");
        }
        writer.WriteLine($"else return {parentClass.GetFullName()}.new");
        writer.CloseBlock("end");
    }
    private const string ResultVarName = "result";
    private void WriteFactoryMethodBodyForUnionModel(CodeMethod codeElement, CodeClass parentClass, CodeParameter parseNodeParameter, LanguageWriter writer)
    {
        writer.WriteLine($"{ResultVarName} = {parentClass.GetFullName()}.new");
        var includeElse = false;
        foreach (var property in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                            .OrderBy(static x => x, CodePropertyTypeForwardComparer)
                                            .ThenBy(static x => x.Name, StringComparer.Ordinal))
        {
            if (property.Type is CodeType propertyType)
                if (propertyType.TypeDefinition is CodeClass && !propertyType.IsCollection)
                {
                    var mappedType = parentClass.DiscriminatorInformation.DiscriminatorMappings.FirstOrDefault(x => x.Value.Name.Equals(propertyType.Name, StringComparison.OrdinalIgnoreCase));
                    writer.WriteLine($"{(includeElse ? "elsif " : string.Empty)}\"{mappedType.Key}\".casecmp({DiscriminatorMappingVarName}) == 0");
                    writer.WriteBlock(lines: $"{ResultVarName}.{property.Name.ToFirstCharacterLowerCase()} = {conventions.GetTypeString(propertyType, codeElement)}.new");
                }
                else if (propertyType.TypeDefinition is CodeClass && propertyType.IsCollection || propertyType.TypeDefinition is null || propertyType.TypeDefinition is CodeEnum)
                {
                    var typeName = conventions.GetTypeString(propertyType, codeElement, true, null);
                    var valueVarName = $"{property.Name.ToFirstCharacterLowerCase()}Value";
                    writer.WriteLine($"{(includeElse ? "elsif " : string.Empty)}{parseNodeParameter.Name.ToFirstCharacterLowerCase()}.{GetDeserializationMethodName(propertyType, codeElement)} is {typeName} {valueVarName}");
                    writer.WriteBlock(lines: $"{ResultVarName}.{property.Name.ToFirstCharacterLowerCase()} = {valueVarName}");
                }
            if (!includeElse)
                includeElse = true;
        }
        writer.WriteLine($"return {ResultVarName}");
    }
    private void WriteFactoryMethodBodyForIntersectionModel(CodeMethod codeElement, CodeClass parentClass, CodeParameter parseNodeParameter, LanguageWriter writer)
    {
        writer.WriteLine($"{ResultVarName} = {parentClass.GetFullName()}.new");
        var includeElse = false;
        foreach (var property in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                            .Where(static x => x.Type is not CodeType propertyType || propertyType.IsCollection || propertyType.TypeDefinition is not CodeClass)
                                            .OrderBy(static x => x, CodePropertyTypeBackwardComparer)
                                            .ThenBy(static x => x.Name, StringComparer.Ordinal))
        {
            if (property.Type is CodeType propertyType)
            {
                var typeName = conventions.GetTypeString(propertyType, codeElement, true);
                var valueVarName = $"{property.Name.ToFirstCharacterLowerCase()}Value";
                writer.WriteLine($"{(includeElse ? "elsif " : string.Empty)}{parseNodeParameter.Name.ToFirstCharacterLowerCase()}.{GetDeserializationMethodName(propertyType, codeElement)} is {typeName} {valueVarName}");
                writer.WriteBlock(lines: $"{ResultVarName}.{property.Name.ToFirstCharacterLowerCase()} = {valueVarName}");
            }
            if (!includeElse)
                includeElse = true;
        }
        var complexProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                            .Where(static x => x.Type is CodeType xType && xType.TypeDefinition is CodeClass && !xType.IsCollection)
                                            .Select(static x => new Tuple<CodeProperty, CodeType>(x, (CodeType)x.Type))
                                            .ToArray();
        if (complexProperties.Length != 0)
        {
            if (includeElse)
            {
                writer.WriteLine("else");
                writer.IncreaseIndent();
            }
            foreach (var property in complexProperties)
                writer.WriteLine($"{ResultVarName}.{property.Item1.Name.ToFirstCharacterLowerCase()} = {conventions.GetTypeString(property.Item2, codeElement)}.new");
            if (includeElse)
            {
                writer.CloseBlock();
            }
        }
        writer.WriteLine($"return {ResultVarName}");
    }
    private const string DiscriminatorMappingVarName = "mappingValue";
    private void WriteFactoryMethodBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        var parseNodeParameter = codeElement.Parameters.OfKind(CodeParameterKind.ParseNode) ?? throw new InvalidOperationException("Factory method should have a ParseNode parameter");

        if (parentClass.DiscriminatorInformation.ShouldWriteParseNodeCheck && !parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
            writer.WriteLine($"{DiscriminatorMappingVarName} = {parseNodeParameter.Name.ToFirstCharacterLowerCase()}.get_child_node(\"{parentClass.DiscriminatorInformation.DiscriminatorPropertyName}\")?.get_string_value");

        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForInheritedType)
            WriteFactoryMethodBodyForInheritedModel(codeElement, parentClass, writer);
        else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType)
            WriteFactoryMethodBodyForUnionModel(codeElement, parentClass, parseNodeParameter, writer);
        else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
            WriteFactoryMethodBodyForIntersectionModel(codeElement, parentClass, parseNodeParameter, writer);
        else
            writer.WriteLine($"return {parentClass.GetFullName()}.new");
    }
    private void WriteRequestBuilderBody(CodeClass parentClass, CodeMethod codeElement, LanguageWriter writer)
    {
        var importSymbol = conventions.GetTypeString(codeElement.ReturnType, parentClass);
        conventions.AddRequestBuilderBody(parentClass, importSymbol, writer, prefix: "return ", pathParameters: codeElement.Parameters.Where(static x => x.IsOfKind(CodeParameterKind.Path)));
    }
    private static void WriteApiConstructorBody(CodeClass parentClass, CodeMethod method, LanguageWriter writer)
    {
        if (parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is not CodeProperty requestAdapterProperty) return;
        var pathParametersProperty = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
        var backingStoreParameter = method.Parameters.OfKind(CodeParameterKind.BackingStore);
        var requestAdapterPropertyName = requestAdapterProperty.Name.ToFirstCharacterLowerCase();
        WriteSerializationRegistration(method.SerializerModules, writer, "register_default_serializer");
        WriteSerializationRegistration(method.DeserializerModules, writer, "register_default_deserializer");
        if (!string.IsNullOrEmpty(method.BaseUrl))
        {
            writer.WriteLine($"if {requestAdapterPropertyName}.base_url.nil?");
            writer.WriteBlock(lines: $"{requestAdapterPropertyName}.base_url = \"{method.BaseUrl}\"");
            if (pathParametersProperty != null)
                writer.WriteLine($"{pathParametersProperty.Name.ToFirstCharacterLowerCase()}.try_add(\"baseurl\", {requestAdapterPropertyName}.base_url)");
        }
        if (backingStoreParameter != null)
            writer.WriteLine($"{requestAdapterPropertyName}.enable_backing_store({backingStoreParameter.Name})");
    }
    private static void WriteSerializationRegistration(HashSet<string> serializationClassNames, LanguageWriter writer, string methodName)
    {
        if (serializationClassNames != null)
            foreach (var serializationClassName in serializationClassNames)
                writer.WriteLine($"ApiClientBuilder.{methodName}({serializationClassName})");
    }
    private void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer)
    {
        foreach (var propWithDefault in parentClass
                                        .Properties
                                        .Where(static x => !string.IsNullOrEmpty(x.DefaultValue) && !x.IsOfKind(CodePropertyKind.UrlTemplate, CodePropertyKind.PathParameters))
                                        // do not apply the default value if the type is composed as the default value may not necessarily which type to use
                                        .Where(static x => x.Type is not CodeType propType || propType.TypeDefinition is not CodeClass propertyClass || propertyClass.OriginalComposedType is null)
                                        .OrderByDescending(static x => x.Kind)
                                        .ThenBy(static x => x.Name))
        {
            var defaultValue = propWithDefault.DefaultValue;
            if (propWithDefault.Type is CodeType propertyType && propertyType.TypeDefinition is CodeEnum)
            {
                defaultValue = $"{conventions.GetTypeString(propWithDefault.Type, currentMethod).TrimEnd('?')}.{defaultValue.Trim('"').CleanupSymbolName().ToFirstCharacterUpperCase()}";
            }
            else if (propWithDefault.Type.IsNullable &&
                defaultValue.TrimQuotes().Equals(NullValueString, StringComparison.OrdinalIgnoreCase))
            { // avoid setting null as a string.
                defaultValue = NullValueString;
            }
            else if (propWithDefault.Type is CodeType propType && propType.Name.Equals("boolean", StringComparison.OrdinalIgnoreCase))
            {
                defaultValue = defaultValue.TrimQuotes();
            }
            writer.WriteLine($"@{propWithDefault.Name.ToFirstCharacterLowerCase()} = {defaultValue}");
        }
        if (parentClass.IsOfKind(CodeClassKind.RequestBuilder) &&
            parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProp &&
            currentMethod.IsOfKind(CodeMethodKind.Constructor) &&
            currentMethod.Parameters.OfKind(CodeParameterKind.PathParameters) is CodeParameter pathParametersParam)
        {
            var pathParameters = currentMethod.Parameters.Where(static x => x.IsOfKind(CodeParameterKind.Path));
            if (pathParameters.Any())
                conventions.AddParametersAssignment(writer,
                                                    pathParametersParam.Type,
                                                    pathParametersParam.Name.ToFirstCharacterLowerCase(),
                                                    pathParametersProp.Name.ToFirstCharacterLowerCase(),
                                                    currentMethod.Parameters
                                                                .Where(static x => x.IsOfKind(CodeParameterKind.Path))
                                                                .Select(static x => (x.Type, string.IsNullOrEmpty(x.SerializationName) ? x.Name : x.SerializationName, x.Name.ToFirstCharacterLowerCase()))
                                                                .ToArray());
        }
    }

    private const string NullValueString = "nil";
    private string DefaultDeserializerValue => $"Hash(String, Proc({conventions.ParseNodeInterfaceName}, Object)).new";
    private void WriteDeserializerBody(bool shouldHide, CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType)
            WriteDeserializerBodyForUnionModel(codeElement, parentClass, writer);
        else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
            WriteDeserializerBodyForIntersectionModel(parentClass, writer);
        else
            WriteDeserializerBodyForInheritedModel(shouldHide, codeElement, parentClass, writer);
    }
    private void WriteDeserializerBodyForUnionModel(CodeMethod method, CodeClass parentClass, LanguageWriter writer)
    {
        var includeElse = false;
        foreach (var otherPropName in parentClass
                                        .GetPropertiesOfKind(CodePropertyKind.Custom)
                                        .Where(static x => !x.ExistsInBaseType)
                                        .Where(static x => x.Type is CodeType propertyType && !propertyType.IsCollection && propertyType.TypeDefinition is CodeClass)
                                        .OrderBy(static x => x, CodePropertyTypeForwardComparer)
                                        .ThenBy(static x => x.Name)
                                        .Select(static x => x.Name.ToFirstCharacterLowerCase()))
        {
            writer.WriteLine($"{(includeElse ? "elsif " : string.Empty)}!@{otherPropName}.nil?");
            writer.WriteBlock(lines: $"return @{otherPropName}.{method.Name.ToFirstCharacterLowerCase()}");
            if (!includeElse)
                includeElse = true;
        }
        writer.WriteLine($"return {DefaultDeserializerValue}");
    }

    private void WriteDeserializerBodyForIntersectionModel(CodeClass parentClass, LanguageWriter writer)
    {
        var complexProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                            .Where(static x => x.Type is CodeType propType && propType.TypeDefinition is CodeClass && !x.Type.IsCollection)
                                            .ToArray();
        if (complexProperties.Length != 0)
        {
            var propertiesNames = complexProperties
                                .Select(static x => x.Name.ToFirstCharacterLowerCase())
                                .OrderBy(static x => x)
                                .ToArray();
            var propertiesNamesAsConditions = propertiesNames
                                .Select(static x => $"!@{x}.nil?")
                                .Aggregate(static (x, y) => $"{x} || {y}");
            writer.WriteLine($"if {propertiesNamesAsConditions}");
            writer.StartBlock();
            var propertiesNamesAsArgument = propertiesNames
                                .Aggregate(static (x, y) => $"{x}, {y}");
            writer.WriteLine($"return ParseNodeHelper.merge_deserializers_for_intersection_wrapper({propertiesNamesAsArgument})");
            writer.CloseBlock();
        }
        writer.WriteLine($"return {DefaultDeserializerValue}");
    }

    private void WriteDeserializerBodyForInheritedModel(bool shouldHide, CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        var parentSerializationInfo = shouldHide ? $"(super.{codeElement.Name.ToFirstCharacterLowerCase()}())" : string.Empty;
        writer.WriteLine($"return {DefaultDeserializerValue}{parentSerializationInfo}");
        writer.StartBlock();
        foreach (var otherProp in parentClass
                                        .GetPropertiesOfKind(CodePropertyKind.Custom)
                                        .Where(static x => !x.ExistsInBaseType)
                                        .OrderBy(static x => x.Name, StringComparer.Ordinal))
        {
            writer.WriteLine($"{{ \"{otherProp.WireName}\", -> {{ @{otherProp.Name.ToFirstCharacterLowerCase()} = n.{GetDeserializationMethodName(otherProp.Type, codeElement)} }} }},");
        }
        writer.CloseBlock("}");
    }

    private string GetDeserializationMethodName(CodeTypeBase propType, CodeMethod method)
    {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyType = conventions.GetTypeString(propType, method, false);
        if (propType is CodeType currentType)
        {
            if (isCollection)
            {
                var collectionMethod = propType.IsArray ? "?.as_array" : "?.as_list";
                if (currentType.TypeDefinition == null)
                    return $"get_collection_of_primitive_values({propertyType}){collectionMethod}";
                else if (currentType.TypeDefinition is CodeEnum)
                    return $"get_collection_of_enum_values({propertyType.TrimEnd('?')}){collectionMethod}";
                else
                    return $"get_collection_of_object_values({propertyType}.create_from_discriminator_value){collectionMethod}";
            }
            else if (currentType.TypeDefinition is CodeEnum enumType)
                return $"get_enum_value({enumType.GetFullName()})";
        }
        return propertyType switch
        {
            "byte[]" => "get_byte_array_value",
            "Time" => "get_datetime_value",
            _ when conventions.IsPrimitiveType(propertyType) => $"get_{propertyType.TrimEnd('?').ToFirstCharacterLowerCase()}_value",
            _ => $"get_object_value({propertyType}.create_from_discriminator_value)",
        };
    }

    protected void WriteRequestExecutorBody(CodeMethod codeElement, RequestParams requestParams, CodeClass parentClass, bool isVoid, string returnTypeWithoutCollectionInformation, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(requestParams);
        ArgumentNullException.ThrowIfNull(parentClass);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");

        var generatorMethodName = parentClass
                                            .Methods
                                            .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod)
                                            ?.Name;
        var parametersList = new CodeParameter?[] { requestParams.requestBody, requestParams.requestContentType, requestParams.requestConfiguration }
                            .Select(static x => x?.Name).Where(static x => x != null).Aggregate(static (x, y) => $"{x}, {y}");
        writer.WriteLine($"request_info = {generatorMethodName}({parametersList})");
        var errorMappingVarName = "default";
        if (codeElement.ErrorMappings.Any())
        {
            errorMappingVarName = "error_mapping";
            writer.WriteLine($"{errorMappingVarName} = Hash(String, Proc({conventions.ParseNodeInterfaceName}, Object)).new");
            writer.StartBlock();
            foreach (var errorMapping in codeElement.ErrorMappings.Where(errorMapping => errorMapping.Value.AllTypes.FirstOrDefault()?.TypeDefinition is CodeClass))
            {
                writer.WriteLine($"{{ \"{errorMapping.Key.ToUpperInvariant()}\", {conventions.GetTypeString(errorMapping.Value, codeElement, false)}.create_from_discriminator_value }},");
            }
            writer.CloseBlock("}");
        }
        var returnTypeCodeType = codeElement.ReturnType as CodeType;
        var returnTypeFactory = returnTypeCodeType?.TypeDefinition is CodeClass || (returnTypeCodeType != null && returnTypeCodeType.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase))
                                ? $", {returnTypeWithoutCollectionInformation}.create_from_discriminator_value"
                                : null;
        var prefix = (isVoid, codeElement.ReturnType.IsCollection) switch
        {
            (true, _) => string.Empty,
            (_, true) => "collection_result = ",
            (_, _) => "return ",
        };
        writer.WriteLine($"{prefix}await request_adapter.{GetSendRequestMethodName(isVoid, codeElement, codeElement.ReturnType)}(request_info{returnTypeFactory}, {errorMappingVarName}, cancellation_token).await");
        if (codeElement.ReturnType.IsCollection)
            writer.WriteLine("return collection_result?.as_list");
    }

    private const string RequestInfoVarName = "request_info";
    private void WriteRequestGeneratorBody(CodeMethod codeElement, RequestParams requestParams, CodeClass currentClass, LanguageWriter writer)
    {
        if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
        if (currentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is not CodeProperty urlTemplateParamsProperty) throw new InvalidOperationException("path parameters property cannot be null");
        if (currentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate) is not CodeProperty urlTemplateProperty) throw new InvalidOperationException("url template property cannot be null");

        var operationName = codeElement.HttpMethod.ToString();
        var urlTemplateValue = codeElement.HasUrlTemplateOverride ? $"\"{codeElement.UrlTemplateOverride}\"" : GetPropertyCall(urlTemplateProperty, "String.new");
        writer.WriteLine($"{RequestInfoVarName} = RequestInformation.new(Method.{operationName?.ToUpperInvariant()}, {urlTemplateValue}, {GetPropertyCall(urlTemplateParamsProperty, "String.new")})");

        if (requestParams.requestConfiguration != null)
            writer.WriteLine($"{RequestInfoVarName}.configure({requestParams.requestConfiguration.Name})");

        if (codeElement.ShouldAddAcceptHeader)
            writer.WriteLine($"{RequestInfoVarName}.headers.try_add(\"Accept\", \"{codeElement.AcceptHeaderValue.SanitizeDoubleQuote()}\")");
        if (requestParams.requestBody != null)
        {
            var suffix = requestParams.requestBody.Type.IsCollection ? "_collection" : string.Empty;
            var sanitizedRequestBodyContentType = codeElement.RequestBodyContentType.SanitizeDoubleQuote();
            if (requestParams.requestBody.Type.Name.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
            {
                if (requestParams.requestContentType is not null)
                    writer.WriteLine($"{RequestInfoVarName}.set_stream_content({requestParams.requestBody.Name}, {requestParams.requestContentType.Name})");
                else if (!string.IsNullOrEmpty(sanitizedRequestBodyContentType))
                    writer.WriteLine($"{RequestInfoVarName}.set_stream_content({requestParams.requestBody.Name}, \"{sanitizedRequestBodyContentType}\")");
            }
            else if (currentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is CodeProperty requestAdapterProperty)
                if (requestParams.requestBody.Type is CodeType bodyType && (bodyType.TypeDefinition is CodeClass || bodyType.Name.Equals("MultipartBody", StringComparison.OrdinalIgnoreCase)))
                    writer.WriteLine($"{RequestInfoVarName}.set_content_from_parsable({requestAdapterProperty.Name.ToFirstCharacterLowerCase()}, \"{sanitizedRequestBodyContentType}\", {requestParams.requestBody.Name})");
                else
                    writer.WriteLine($"{RequestInfoVarName}.set_content_from_scalar{suffix}({requestAdapterProperty.Name.ToFirstCharacterLowerCase()}, \"{sanitizedRequestBodyContentType}\", {requestParams.requestBody.Name})");
        }

        writer.WriteLine($"return {RequestInfoVarName}");
    }

    private static string GetPropertyCall(CodeProperty property, string defaultValue) => property == null ? defaultValue : $"@{property.Name.ToFirstCharacterLowerCase()}";

    private void WriteSerializerBody(bool shouldHide, CodeMethod method, CodeClass parentClass, LanguageWriter writer)
    {
        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType)
            WriteSerializerBodyForUnionModel(method, parentClass, writer);
        else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
            WriteSerializerBodyForIntersectionModel(method, parentClass, writer);
        else
            WriteSerializerBodyForInheritedModel(shouldHide, method, parentClass, writer);

        if (parentClass.GetPropertyOfKind(CodePropertyKind.AdditionalData) is CodeProperty additionalDataProperty)
            writer.WriteLine($"writer.write_additional_data(@{additionalDataProperty.Name.ToFirstCharacterLowerCase()})");
    }

    private void WriteSerializerBodyForInheritedModel(bool shouldHide, CodeMethod method, CodeClass parentClass, LanguageWriter writer)
    {
        if (shouldHide)
            writer.WriteLine("super.serialize(writer)");
        foreach (var otherProp in parentClass
                                        .GetPropertiesOfKind(CodePropertyKind.Custom)
                                        .Where(static x => !x.ExistsInBaseType && !x.ReadOnly)
                                        .OrderBy(static x => x.Name))
        {
            var serializationMethodName = GetSerializationMethodName(otherProp.Type, method);
            writer.WriteLine($"writer.{serializationMethodName}(\"{otherProp.WireName}\", @{otherProp.Name.ToFirstCharacterLowerCase()})");
        }
    }

    private void WriteSerializerBodyForUnionModel(CodeMethod method, CodeClass parentClass, LanguageWriter writer)
    {
        var includeElse = false;
        foreach (var otherProp in parentClass
                                        .GetPropertiesOfKind(CodePropertyKind.Custom)
                                        .Where(static x => !x.ExistsInBaseType)
                                        .OrderBy(static x => x, CodePropertyTypeForwardComparer)
                                        .ThenBy(static x => x.Name))
        {
            writer.WriteLine($"{(includeElse ? "elsif " : string.Empty)}!@{otherProp.Name.ToFirstCharacterLowerCase()}.nil?");
            writer.WriteBlock(lines: $"writer.{GetSerializationMethodName(otherProp.Type, method)}(nil, @{otherProp.Name.ToFirstCharacterLowerCase()})");
            if (!includeElse)
                includeElse = true;
        }
    }

    private void WriteSerializerBodyForIntersectionModel(CodeMethod method, CodeClass parentClass, LanguageWriter writer)
    {
        var includeElse = false;
        foreach (var otherProp in parentClass
                                        .GetPropertiesOfKind(CodePropertyKind.Custom)
                                        .Where(static x => !x.ExistsInBaseType)
                                        .Where(static x => x.Type is not CodeType propertyType || propertyType.IsCollection || propertyType.TypeDefinition is not CodeClass)
                                        .OrderBy(static x => x, CodePropertyTypeBackwardComparer)
                                        .ThenBy(static x => x.Name))
        {
            writer.WriteLine($"{(includeElse ? "elsif " : string.Empty)}!@{otherProp.Name.ToFirstCharacterLowerCase()}.nil?");
            writer.WriteBlock(lines: $"writer.{GetSerializationMethodName(otherProp.Type, method)}(nil, @{otherProp.Name.ToFirstCharacterLowerCase()})");
            if (!includeElse)
                includeElse = true;
        }
        var complexProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                            .Where(static x => x.Type is CodeType propType && propType.TypeDefinition is CodeClass && !x.Type.IsCollection)
                                            .ToArray();
        if (complexProperties.Length != 0)
        {
            if (includeElse)
            {
                writer.WriteLine("else");
                writer.IncreaseIndent();
            }
            var propertiesNames = complexProperties
                                .Select(static x => x.Name.ToFirstCharacterLowerCase())
                                .OrderBy(static x => x)
                                .Aggregate(static (x, y) => $"{x}, {y}");
            writer.WriteLine($"writer.{GetSerializationMethodName(complexProperties.First().Type, method)}(nil, {propertiesNames})");
            if (includeElse)
            {
                writer.CloseBlock();
            }
        }
    }

    protected virtual void WriteCommandBuilderBody(CodeMethod codeElement, CodeClass parentClass, RequestParams requestParams, bool isVoid, string returnType, LanguageWriter writer)
    {
        throw new InvalidOperationException("CommandBuilder methods are not implemented in this SDK. They're currently only supported in the shell language.");
    }

    protected string GetSendRequestMethodName(bool isVoid, CodeElement currentElement, CodeTypeBase returnType)
    {
        ArgumentNullException.ThrowIfNull(returnType);
        var returnTypeName = conventions.GetTypeString(returnType, currentElement, false);
        var isStream = conventions.StreamTypeName.Equals(returnTypeName, StringComparison.OrdinalIgnoreCase);
        var isEnum = returnType is CodeType codeType && codeType.TypeDefinition is CodeEnum;
        if (isVoid) return "send_no_content_async";
        else if (isStream || conventions.IsPrimitiveType(returnTypeName) || isEnum)
            if (returnType.IsCollection)
                return $"send_primitive_collection_async({returnTypeName})";
            else
                return $"send_primitive_async({returnTypeName})";
        else if (returnType.IsCollection) return $"send_collection_async({returnTypeName})";
        else return $"send_async({returnTypeName})";
    }

    private void WriteMethodDocumentation(CodeMethod code, LanguageWriter writer)
    {
        conventions.WriteLongDescription(code, writer);
        if (!"void".Equals(code.ReturnType.Name, StringComparison.OrdinalIgnoreCase) && code.Kind is not CodeMethodKind.ClientConstructor or CodeMethodKind.Constructor)
            conventions.WriteAdditionalDescriptionItem($"# Returns a {conventions.GetTypeStringForDocumentation(code.ReturnType, code)}", writer);
        foreach (var paramWithDescription in code.Parameters
                                                .Where(static x => x.Documentation.DescriptionAvailable)
                                                .OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase))
            conventions.WriteShortDescription(paramWithDescription, writer, $"# @param {paramWithDescription.Name.ToFirstCharacterLowerCase()} ", "");
        WriteThrownExceptions(code, writer);
        conventions.WriteDeprecationAttribute(code, writer);
    }

    private void WriteThrownExceptions(CodeMethod element, LanguageWriter writer)
    {
        if (element.Kind is not CodeMethodKind.RequestExecutor) return;
        foreach (var exception in element.ErrorMappings)
        {
            var statusCode = exception.Key.ToUpperInvariant() switch
            {
                "XXX" => "4XX or 5XX",
                _ => exception.Key,
            };
            conventions.WriteAdditionalDescriptionItem($"# @raise [{conventions.GetTypeString(exception.Value, element)}] When receiving a {statusCode} status code", writer);
        }
    }

    private static readonly BaseCodeParameterOrderComparer parameterOrderComparer = new();
    private static string GetBaseSuffix(bool isConstructor, bool inherits, CodeClass parentClass, CodeMethod currentMethod)
    {
        if (isConstructor && inherits)
        {
            if (parentClass.IsOfKind(CodeClassKind.RequestBuilder) && parentClass.Properties.FirstOrDefaultOfKind(CodePropertyKind.UrlTemplate) is CodeProperty urlTemplateProperty &&
                !string.IsNullOrEmpty(urlTemplateProperty.DefaultValue))
            {
                var thirdParameterName = string.Empty;
                if (currentMethod.Parameters.OfKind(CodeParameterKind.PathParameters) is CodeParameter pathParametersParameter)
                    thirdParameterName = $", {pathParametersParameter.Name}";
                else if (currentMethod.Parameters.OfKind(CodeParameterKind.RawUrl) is CodeParameter rawUrlParameter)
                    thirdParameterName = $", {rawUrlParameter.Name}";
                else if (parentClass.Properties.FirstOrDefaultOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProperty && !string.IsNullOrEmpty(pathParametersProperty.DefaultValue))
                    thirdParameterName = $", {pathParametersProperty.DefaultValue}";
                if (currentMethod.Parameters.OfKind(CodeParameterKind.RequestAdapter) is CodeParameter requestAdapterParameter)
                {
                    return $" : super({requestAdapterParameter.Name.ToFirstCharacterLowerCase()}, {urlTemplateProperty.DefaultValue}{thirdParameterName})";
                }
                else if (parentClass.StartBlock?.Inherits?.Name?.Contains("CliRequestBuilder", StringComparison.Ordinal) == true)
                {
                    // CLI uses a different base class.
                    return $" : super({urlTemplateProperty.DefaultValue}{thirdParameterName})";
                }
            }
            return " : super()";
        }

        return string.Empty;
    }

    private void WriteMethodPrototype(CodeMethod code, CodeClass parentClass, LanguageWriter writer, string returnType, bool inherits, bool isVoid)
    {
        var staticModifier = code.IsStatic ? "self." : string.Empty;
        var hideModifier = (inherits, code.Kind) switch
        {
            (true, CodeMethodKind.Serializer or CodeMethodKind.Deserializer) => "override ",
            (false, CodeMethodKind.Serializer or CodeMethodKind.Deserializer) => "virtual ",
            (true, CodeMethodKind.Factory) => "new ",
            _ => string.Empty
        };
        var isConstructor = code.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor, CodeMethodKind.RawUrlConstructor);
        var asyncPrefix = code.IsAsync ? "async " : string.Empty;
        var voidCorrectedTaskReturnType = code.IsAsync && isVoid ? string.Empty : returnType;
        if (code.ReturnType.IsArray && code.IsOfKind(CodeMethodKind.RequestExecutor))
            voidCorrectedTaskReturnType = $"Array({voidCorrectedTaskReturnType.StripArraySuffix()})";
        var completeReturnType = isConstructor ? string.Empty : $" : {asyncPrefix}{voidCorrectedTaskReturnType}";
        var baseSuffix = GetBaseSuffix(isConstructor, inherits, parentClass, code);
        var parameters = string.Join(", ", code.Parameters.OrderBy(x => x, parameterOrderComparer).Select(p => conventions.GetParameterSignature(p, code)).ToList());
        var methodName = isConstructor ? "initialize" : code.Name.ToSnakeCase();
        var abstractModifier = code.IsAbstract() ? "abstract " : string.Empty;
        writer.WriteLine($"{conventions.GetAccessModifier(code.Access)} {staticModifier}{abstractModifier}{hideModifier}{methodName}({parameters}){completeReturnType}{baseSuffix}");
        writer.WriteLine("def");
    }

    private string GetSerializationMethodName(CodeTypeBase propType, CodeMethod method)
    {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyType = conventions.GetTypeString(propType, method, false);
        if (propType is CodeType currentType)
        {
            if (isCollection)
                if (currentType.TypeDefinition == null)
                    return $"write_collection_of_primitive_values({propertyType})";
                else if (currentType.TypeDefinition is CodeEnum)
                    return $"write_collection_of_enum_values({propertyType.TrimEnd('?')})";
                else
                    return $"write_collection_of_object_values({propertyType})";
            else if (currentType.TypeDefinition is CodeEnum enumType)
                return $"write_enum_value({enumType.GetFullName()})";
        }
        return propertyType switch
        {
            "byte[]" => "write_byte_array_value",
            "Time" => "write_datetime_value",
            _ when conventions.IsPrimitiveType(propertyType) => $"write_{propertyType.TrimEnd('?').ToFirstCharacterLowerCase()}_value",
            _ => $"write_object_value({propertyType})",
        };
    }
}
