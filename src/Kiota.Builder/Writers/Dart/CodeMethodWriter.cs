using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.OrderComparers;
using static Kiota.Builder.CodeDOM.CodeTypeBase;

namespace Kiota.Builder.Writers.Dart;

public class CodeMethodWriter : BaseElementWriter<CodeMethod, DartConventionService>
{
    public CodeMethodWriter(DartConventionService conventionService) : base(conventionService)
    {

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

        HandleMethodKind(codeElement, writer, inherits, parentClass, isVoid);
        var isConstructor = codeElement.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor, CodeMethodKind.RawUrlConstructor);

        if (HasEmptyConstructorBody(codeElement, parentClass, isConstructor))
        {
            writer.DecreaseIndent();
        }
        else
        {
            if (isConstructor && !inherits && parentClass.Properties.Where(static x => x.Kind is CodePropertyKind.AdditionalData).Any() && !parentClass.IsErrorDefinition && !parentClass.Properties.Where(static x => x.Kind is CodePropertyKind.BackingStore).Any())
            {
                writer.DecreaseIndent();
            }
            else if (isConstructor && parentClass.IsErrorDefinition)
            {
                if (parentClass.Properties.Where(static x => x.IsOfKind(CodePropertyKind.AdditionalData)).Any() && parentClass.Properties.Where(static x => x.IsOfKind(CodePropertyKind.BackingStore)).Any())
                {
                    writer.CloseBlock("}) {additionalData = {};}");
                }
                else
                {
                    writer.CloseBlock("});");
                }
            }
            else
            {
                writer.CloseBlock();
            }
        }
    }

    private static bool HasEmptyConstructorBody(CodeMethod codeElement, CodeClass parentClass, bool isConstructor)
    {
        if (parentClass.IsOfKind(CodeClassKind.Model) && codeElement.IsOfKind(CodeMethodKind.Constructor) && !parentClass.IsErrorDefinition)
        {
            return parentClass.Properties.All(prop => string.IsNullOrEmpty(prop.DefaultValue));
        }
        var hasBody = codeElement.Parameters.Any(p => !p.IsOfKind(CodeParameterKind.RequestAdapter) && !p.IsOfKind(CodeParameterKind.PathParameters));
        return isConstructor && parentClass.IsOfKind(CodeClassKind.RequestBuilder) && !codeElement.IsOfKind(CodeMethodKind.ClientConstructor) && (!hasBody || codeElement.IsOfKind(CodeMethodKind.RawUrlConstructor));
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
            case CodeMethodKind.IndexerBackwardCompatibility:
            case CodeMethodKind.RequestBuilderWithParameters:
                WriteRequestBuilderBody(parentClass, codeElement, writer);
                break;
            case CodeMethodKind.QueryParametersMapper:
                WriteQueryparametersBody(parentClass, writer);
                break;
            case CodeMethodKind.Getter:
            case CodeMethodKind.Setter:
                throw new InvalidOperationException("getters and setters are automatically added on fields in Dart");
            case CodeMethodKind.RequestBuilderBackwardCompatibility:
                throw new InvalidOperationException("RequestBuilderBackwardCompatibility is not supported as the request builders are implemented by properties.");
            case CodeMethodKind.ErrorMessageOverride:
                throw new InvalidOperationException("ErrorMessageOverride is not supported as the error message is implemented by a property.");
            case CodeMethodKind.CommandBuilder:
                throw new InvalidOperationException("CommandBuilder methods are not implemented in this SDK. They're currently only supported in the shell language.");
            case CodeMethodKind.Factory:
                WriteFactoryMethodBody(codeElement, parentClass, writer);
                break;
            case CodeMethodKind.Custom:
                WriteCustomMethodBody(codeElement, parentClass, writer);
                break;
            case CodeMethodKind.ComposedTypeMarker:
                throw new InvalidOperationException("ComposedTypeMarker is not required as interface is explicitly implemented.");
            default:
                writer.WriteLine("return null;");
                break;
        }
    }
    private void WriteRawUrlBuilderBody(CodeClass parentClass, CodeMethod codeElement, LanguageWriter writer)
    {
        var rawUrlParameter = codeElement.Parameters.OfKind(CodeParameterKind.RawUrl) ?? throw new InvalidOperationException("RawUrlBuilder method should have a RawUrl parameter");
        var requestAdapterProperty = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) ?? throw new InvalidOperationException("RawUrlBuilder method should have a RequestAdapter property");
        writer.WriteLine($"return {parentClass.Name}.withUrl({rawUrlParameter.Name}, {requestAdapterProperty.Name});");
    }
    private static readonly CodePropertyTypeComparer CodePropertyTypeForwardComparer = new();
    private static readonly CodePropertyTypeComparer CodePropertyTypeBackwardComparer = new(true);
    private void WriteFactoryMethodBodyForInheritedModel(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        writer.StartBlock($"return switch({DiscriminatorMappingVarName}) {{");
        foreach (var mappedType in parentClass.DiscriminatorInformation.DiscriminatorMappings)
        {
            writer.WriteLine($"'{mappedType.Key}' => {conventions.GetTypeString(mappedType.Value.AllTypes.First(), codeElement)}(),");
        }
        writer.WriteLine($"_ => {parentClass.Name}(),");
        writer.CloseBlock("};");
    }
    private const string ResultVarName = "result";
    private void WriteFactoryMethodBodyForUnionModel(CodeMethod codeElement, CodeClass parentClass, CodeParameter parseNodeParameter, LanguageWriter writer)
    {
        writer.WriteLine($"var {ResultVarName} = {parentClass.Name}();");

        if (parentClass.GetPropertiesOfKind(CodePropertyKind.Custom).Where(static x => x.Type is CodeType cType && cType.TypeDefinition is CodeClass && !cType.IsCollection).Any())
        {
            var discriminatorPropertyName = parentClass.DiscriminatorInformation.DiscriminatorPropertyName;
            discriminatorPropertyName = discriminatorPropertyName.StartsWith('$') ? "\\" + discriminatorPropertyName : discriminatorPropertyName;
            writer.WriteLine($"var {DiscriminatorMappingVarName} = {parseNodeParameter.Name}.getChildNode('{discriminatorPropertyName}')?.getStringValue();");
        }
        var includeElse = false;
        foreach (var property in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                            .OrderBy(static x => x, CodePropertyTypeForwardComparer)
                                            .ThenBy(static x => x.Name, StringComparer.Ordinal))
        {
            if (property.Type is CodeType propertyType)
                if (propertyType.TypeDefinition is CodeClass && !propertyType.IsCollection)
                {
                    var mappedType = parentClass.DiscriminatorInformation.DiscriminatorMappings.FirstOrDefault(x => x.Value.Name.Equals(propertyType.Name, StringComparison.OrdinalIgnoreCase));
                    writer.StartBlock($"{(includeElse ? "else " : string.Empty)}if('{mappedType.Key}' == {DiscriminatorMappingVarName}) {{");
                    writer.WriteLine($"{ResultVarName}.{property.Name} = {conventions.GetTypeString(propertyType, codeElement)}();");
                    writer.CloseBlock();
                }
                else if (propertyType.TypeDefinition is CodeClass && propertyType.IsCollection || propertyType.TypeDefinition is null || propertyType.TypeDefinition is CodeEnum)
                {
                    var typeName = conventions.GetTypeString(propertyType, codeElement, true, false);
                    var check = propertyType.IsCollection ? ".isNotEmpty" : $" is {typeName}";
                    writer.StartBlock($"{(includeElse ? "else " : string.Empty)}if({parseNodeParameter.Name}.{GetDeserializationMethodName(propertyType, codeElement)}{check}) {{");
                    writer.WriteLine($"{ResultVarName}.{property.Name} = {parseNodeParameter.Name}.{GetDeserializationMethodName(propertyType, codeElement)};");
                    writer.CloseBlock();
                }
            if (!includeElse)
                includeElse = true;
        }
        writer.WriteLine($"return {ResultVarName};");
    }
    private void WriteFactoryMethodBodyForIntersectionModel(CodeMethod codeElement, CodeClass parentClass, CodeParameter parseNodeParameter, LanguageWriter writer)
    {
        writer.WriteLine($"var {ResultVarName} = {parentClass.Name}();");
        var includeElse = false;
        foreach (var property in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                            .Where(static x => x.Type is not CodeType propertyType || propertyType.IsCollection || propertyType.TypeDefinition is not CodeClass)
                                            .OrderBy(static x => x, CodePropertyTypeBackwardComparer)
                                            .ThenBy(static x => x.Name, StringComparer.Ordinal))
        {
            if (property.Type is CodeType propertyType)
            {
                var check = propertyType.IsCollection ? ".isNotEmpty" : " != null";
                writer.StartBlock($"{(includeElse ? "else " : string.Empty)}if({parseNodeParameter.Name}.{GetDeserializationMethodName(propertyType, codeElement)}{check}) {{");
                writer.WriteLine($"{ResultVarName}.{property.Name} = {parseNodeParameter.Name}.{GetDeserializationMethodName(propertyType, codeElement)};");
                writer.CloseBlock();
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
                writer.StartBlock("else {");
            }
            foreach (var property in complexProperties)
                writer.WriteLine($"{ResultVarName}.{property.Item1.Name} = {conventions.GetTypeString(property.Item2, codeElement)}();");
            if (includeElse)
            {
                writer.CloseBlock();
            }
        }
        writer.WriteLine($"return {ResultVarName};");
    }

    private const string DiscriminatorMappingVarName = "mappingValue";
    private void WriteFactoryMethodBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        var parseNodeParameter = codeElement.Parameters.OfKind(CodeParameterKind.ParseNode) ?? throw new InvalidOperationException("Factory method should have a ParseNode parameter");

        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForInheritedType)
        {
            var discriminatorPropertyName = parentClass.DiscriminatorInformation.DiscriminatorPropertyName;
            discriminatorPropertyName = discriminatorPropertyName.StartsWith('$') ? "\\" + discriminatorPropertyName : discriminatorPropertyName;
            writer.WriteLine($"var {DiscriminatorMappingVarName} = {parseNodeParameter.Name}.getChildNode('{discriminatorPropertyName}')?.getStringValue();");
            WriteFactoryMethodBodyForInheritedModel(codeElement, parentClass, writer);
        }
        else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType)
            WriteFactoryMethodBodyForUnionModel(codeElement, parentClass, parseNodeParameter, writer);
        else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
            WriteFactoryMethodBodyForIntersectionModel(codeElement, parentClass, parseNodeParameter, writer);
        else if (parentClass.IsErrorDefinition && parentClass.Properties.Where(static x => x.IsOfKind(CodePropertyKind.AdditionalData)).Any() && !parentClass.Properties.Where(static x => x.IsOfKind(CodePropertyKind.BackingStore)).Any())
        {
            writer.WriteLine($"return {parentClass.Name}(additionalData: {{}});");
        }
        else
            writer.WriteLine($"return {parentClass.Name}();");
    }

    private void WriteRequestBuilderBody(CodeClass parentClass, CodeMethod codeElement, LanguageWriter writer)
    {
        var importSymbol = conventions.GetTypeString(codeElement.ReturnType, parentClass);
        conventions.AddRequestBuilderBody(parentClass, importSymbol, writer, prefix: "return ", pathParameters: codeElement.Parameters.Where(static x => x.IsOfKind(CodeParameterKind.Path)), customParameters: codeElement.Parameters.Where(static x => x.IsOfKind(CodeParameterKind.Custom)));
    }
    private static void WriteApiConstructorBody(CodeClass parentClass, CodeMethod method, LanguageWriter writer)
    {
        if (parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is not CodeProperty requestAdapterProperty) return;
        var pathParametersProperty = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
        var backingStoreParameter = method.Parameters.OfKind(CodeParameterKind.BackingStore);
        var requestAdapterPropertyName = requestAdapterProperty.Name;
        WriteSerializationRegistration(method.SerializerModules, writer, "registerDefaultSerializer");
        WriteSerializationRegistration(method.DeserializerModules, writer, "registerDefaultDeserializer");
        if (!string.IsNullOrEmpty(method.BaseUrl))
        {
            writer.StartBlock($"if ({requestAdapterPropertyName}.baseUrl == null || {requestAdapterPropertyName}.baseUrl!.isEmpty) {{");
            writer.WriteLine($"{requestAdapterPropertyName}.baseUrl = '{method.BaseUrl}';");
            writer.CloseBlock();
            if (pathParametersProperty != null)
                writer.WriteLine($"{pathParametersProperty.Name}['baseurl'] = {requestAdapterPropertyName}.baseUrl;");
        }
        if (backingStoreParameter != null)
        {
            writer.StartBlock($"if ({backingStoreParameter.Name} != null) {{");
            writer.WriteLine($"{requestAdapterPropertyName}.enableBackingStore({backingStoreParameter.Name});");
            writer.CloseBlock();
        }
    }
    private static void WriteSerializationRegistration(HashSet<string> serializationClassNames, LanguageWriter writer, string methodName)
    {
        if (serializationClassNames != null)
            foreach (var serializationClassName in serializationClassNames)
                writer.WriteLine($"ApiClientBuilder.{methodName}({serializationClassName}.new);");
    }
    private void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer)
    {
        if (parentClass.IsErrorDefinition)
        {
            WriteErrorClassConstructor(parentClass, writer);
        }
        else
        {
            var separator = ',';
            var propWithDefaults = parentClass.Properties
                                        .Where(static x => !string.IsNullOrEmpty(x.DefaultValue) && !x.IsOfKind(CodePropertyKind.UrlTemplate, CodePropertyKind.PathParameters, CodePropertyKind.BackingStore))
                                        // do not apply the default value if the type is composed as the default value may not necessarily which type to use
                                        .Where(static x => x.Type is not CodeType propType || propType.TypeDefinition is not CodeClass propertyClass || propertyClass.OriginalComposedType is null)
                                        .OrderByDescending(static x => x.Kind)
                                        .ThenBy(static x => x.Name).ToArray();
            var lastOption = propWithDefaults.LastOrDefault();

            foreach (var propWithDefault in propWithDefaults)
            {
                var defaultValue = propWithDefault.DefaultValue;
                if (propWithDefault == lastOption)
                {
                    separator = ';';
                }
                if (propWithDefault.Type is CodeType propertyType && propertyType.TypeDefinition is CodeEnum)
                {
                    defaultValue = $"{conventions.GetTypeString(propWithDefault.Type, currentMethod).TrimEnd('?')}.{defaultValue}";
                }
                else if (propWithDefault.Type is CodeType propertyType2)
                {
                    defaultValue = defaultValue.Trim('"');
                    if (propertyType2.Name.Equals("String", StringComparison.Ordinal))
                    {
                        defaultValue = $"'{defaultValue}'";
                    }
                }
                writer.WriteLine($"{propWithDefault.Name} = {defaultValue}{separator}");
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
                                                        pathParametersParam.Name,
                                                        pathParametersProp.Name,
                                                        currentMethod.Parameters
                                                                    .Where(static x => x.IsOfKind(CodeParameterKind.Path))
                                                                    .Select(static x => (x.Type, string.IsNullOrEmpty(x.SerializationName) ? x.Name : x.SerializationName, x.Name))
                                                                    .ToArray());
            }
        }
    }

    private void WriteErrorClassConstructor(CodeClass parentClass, LanguageWriter writer)
    {
        foreach (string prop in DartConventionService.ErrorClassProperties)
        {
            writer.WriteLine($"super.{prop},");
        }
        if (!parentClass.Properties.Where(static x => x.IsOfKind(CodePropertyKind.BackingStore)).Any())
        {
            foreach (CodeProperty prop in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom, CodePropertyKind.AdditionalData))
            {
                var required = prop.Type.IsNullable ? "" : "required ";

                if (!conventions.ErrorClassPropertyExistsInSuperClass(prop))
                {
                    writer.WriteLine($"{required}this.{prop.Name},");
                }
            }
        }
    }

    private string DefaultDeserializerReturnInstance => $"<String, void Function({conventions.ParseNodeInterfaceName})>";
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
                                        .Select(static x => x.Name))
        {
            writer.StartBlock($"{(includeElse ? "else " : string.Empty)}if({otherPropName} != null) {{");
            writer.WriteLine($"return {otherPropName}!.{method.Name}();");
            writer.CloseBlock();
            if (!includeElse)
                includeElse = true;
        }
        writer.WriteLine($"return {DefaultDeserializerReturnInstance}{{}};");
    }
    private const string DeserializerName = "deserializers";

    private void WriteDeserializerBodyForIntersectionModel(CodeClass parentClass, LanguageWriter writer)
    {

        writer.WriteLine($"var {DeserializerName} = {DefaultDeserializerReturnInstance}{{}};");
        var complexProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                            .Where(static x => x.Type is CodeType propType && propType.TypeDefinition is CodeClass && !x.Type.IsCollection)
                                            .ToArray();
        foreach (CodeProperty prop in complexProperties)
        {
            writer.WriteLine($"if({prop.Name} != null){{{prop.Name}!.getFieldDeserializers().forEach((k,v) => {DeserializerName}.putIfAbsent(k, ()=>v));}}");
        }
        writer.WriteLine($"return {DeserializerName};");
    }
    private const string DeserializerVarName = "deserializerMap";
    private void WriteDeserializerBodyForInheritedModel(bool shouldHide, CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        var fieldToSerialize = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom, CodePropertyKind.ErrorMessageOverride).ToArray();
        if (shouldHide)
        {
            writer.WriteLine($"var {DeserializerVarName} = " + "super.getFieldDeserializers();");
        }
        else
        {
            writer.WriteLine($"var {DeserializerVarName} = {DefaultDeserializerReturnInstance}{{}};");
        }

        if (fieldToSerialize.Length != 0)
        {
            fieldToSerialize
                    .Where(x => !x.ExistsInBaseType && !conventions.ErrorClassPropertyExistsInSuperClass(x))
                    .OrderBy(static x => x.Name)
                    .Select(x =>
                        $"{DeserializerVarName}['{x.WireName}'] = (node) => {x.Name} = node.{GetDeserializationMethodName(x.Type, codeElement)};")
                    .ToList()
                    .ForEach(x => writer.WriteLine(x));
        }
        writer.WriteLine($"return {DeserializerVarName};");
    }
    private string GetDeserializationMethodName(CodeTypeBase propType, CodeMethod method)
    {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyType = conventions.GetTypeString(propType, method, false);
        if (propType is CodeType currentType)
        {
            if (isCollection)
            {
                var collectionMethod = "";
                if (currentType.TypeDefinition == null)
                    return $"getCollectionOfPrimitiveValues<{propertyType.TrimEnd(DartConventionService.NullableMarker)}>(){collectionMethod}";
                else if (currentType.TypeDefinition is CodeEnum enumType)
                {
                    var typeName = enumType.Name;
                    return $"getCollectionOfEnumValues<{typeName}>((stringValue) => {typeName}.values.where((enumVal) => enumVal.value == stringValue).firstOrNull)";
                }
                else
                    return $"getCollectionOfObjectValues<{propertyType}>({propertyType}.createFromDiscriminatorValue){collectionMethod}";
            }
            else if (currentType.TypeDefinition is CodeEnum enumType)
            {
                var typeName = enumType.Name;
                return $"getEnumValue<{typeName}>((stringValue) => {typeName}.values.where((enumVal) => enumVal.value == stringValue).firstOrNull)";
            }
        }
        return propertyType switch
        {
            "Iterable<int>" => "getCollectionOfPrimitiveValues<int>()",
            "UuidValue" => "getGuidValue()",
            "byte[]" => "getByteArrayValue()",
            _ when conventions.IsPrimitiveType(propertyType) => $"get{propertyType.TrimEnd(DartConventionService.NullableMarker).ToFirstCharacterUpperCase()}Value()",
            _ => $"getObjectValue<{propertyType.ToFirstCharacterUpperCase()}>({propertyType}.createFromDiscriminatorValue)",
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
        writer.WriteLine($"var requestInfo = {generatorMethodName}({parametersList});");
        var errorMappingVarName = "{}";
        if (codeElement.ErrorMappings.Any())
        {
            errorMappingVarName = "errorMapping";
            writer.StartBlock($"final {errorMappingVarName} = <String, ParsableFactory<Parsable>>{{");
            foreach (var errorMapping in codeElement.ErrorMappings.Where(errorMapping => errorMapping.Value.AllTypes.FirstOrDefault()?.TypeDefinition is CodeClass))
            {
                writer.WriteLine($"'{errorMapping.Key.ToUpperInvariant()}' :  {conventions.GetTypeString(errorMapping.Value, codeElement, false)}.createFromDiscriminatorValue,");
            }
            writer.CloseBlock("};");
        }
        var returnTypeCodeType = codeElement.ReturnType as CodeType;
        var returnTypeFactory = returnTypeCodeType?.TypeDefinition is CodeClass || (returnTypeCodeType != null && returnTypeCodeType.Name.Equals(KiotaBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase))
                                ? $", {returnTypeWithoutCollectionInformation}.createFromDiscriminatorValue"
                                : null;
        writer.WriteLine($"return await requestAdapter.{GetSendRequestMethodName(isVoid, codeElement, codeElement.ReturnType)}(requestInfo{returnTypeFactory}, {errorMappingVarName});");
    }
    private const string RequestInfoVarName = "requestInfo";
    private void WriteRequestGeneratorBody(CodeMethod codeElement, RequestParams requestParams, CodeClass currentClass, LanguageWriter writer)
    {
        if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
        if (currentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is not CodeProperty urlTemplateParamsProperty) throw new InvalidOperationException("path parameters property cannot be null");
        if (currentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate) is not CodeProperty urlTemplateProperty) throw new InvalidOperationException("url template property cannot be null");

        var operationName = codeElement.HttpMethod.ToString();
        var urlTemplateValue = codeElement.HasUrlTemplateOverride ? $"'{codeElement.UrlTemplateOverride}'" : GetPropertyCall(urlTemplateProperty, "''");
        writer.WriteLine($"var {RequestInfoVarName} = RequestInformation(httpMethod : HttpMethod.{operationName?.ToLowerInvariant()}, {urlTemplateProperty.Name} : {urlTemplateValue}, {urlTemplateParamsProperty.Name} :  {GetPropertyCall(urlTemplateParamsProperty, "string.Empty")});");

        if (requestParams.requestConfiguration != null && requestParams.requestConfiguration.Type is CodeType paramType)
        {
            var parameterClassName = paramType.GenericTypeParameterValues.First().Name;
            writer.WriteLine($"{RequestInfoVarName}.configure<{parameterClassName}>({requestParams.requestConfiguration.Name}, () => {parameterClassName}());");
        }

        if (codeElement.ShouldAddAcceptHeader)
            writer.WriteLine($"{RequestInfoVarName}.headers.put('Accept', '{codeElement.AcceptHeaderValue}');");
        if (requestParams.requestBody != null)
        {
            var suffix = requestParams.requestBody.Type.IsCollection ? "Collection" : string.Empty;
            if (requestParams.requestBody.Type.Name.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
            {
                if (requestParams.requestContentType is not null)
                    writer.WriteLine($"{RequestInfoVarName}.setStreamContent({requestParams.requestBody.Name}, {requestParams.requestContentType.Name});");
                else if (!string.IsNullOrEmpty(codeElement.RequestBodyContentType))
                    writer.WriteLine($"{RequestInfoVarName}.setStreamContent({requestParams.requestBody.Name}, '{codeElement.RequestBodyContentType}');");
            }
            else if (currentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is CodeProperty requestAdapterProperty)
                if (requestParams.requestBody.Type is CodeType bodyType && (bodyType.TypeDefinition is CodeClass || bodyType.Name.Equals("MultipartBody", StringComparison.OrdinalIgnoreCase)))
                    writer.WriteLine($"{RequestInfoVarName}.setContentFromParsable{suffix}({requestAdapterProperty.Name}, '{codeElement.RequestBodyContentType}', {requestParams.requestBody.Name});");
                else
                    writer.WriteLine($"{RequestInfoVarName}.setContentFromScalar{suffix}({requestAdapterProperty.Name}, '{codeElement.RequestBodyContentType}', {requestParams.requestBody.Name});");
        }

        writer.WriteLine($"return {RequestInfoVarName};");
    }
    private static string GetPropertyCall(CodeProperty property, string defaultValue) => property == null ? defaultValue : $"{property.Name}";
    private void WriteSerializerBody(bool shouldHide, CodeMethod method, CodeClass parentClass, LanguageWriter writer)
    {
        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType)
            WriteSerializerBodyForUnionModel(method, parentClass, writer);
        else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
            WriteSerializerBodyForIntersectionModel(method, parentClass, writer);
        else
            WriteSerializerBodyForInheritedModel(shouldHide, method, parentClass, writer);

        if (parentClass.GetPropertyOfKind(CodePropertyKind.AdditionalData) is CodeProperty additionalDataProperty)
            writer.WriteLine($"writer.writeAdditionalData({additionalDataProperty.Name});");
    }
    private void WriteSerializerBodyForInheritedModel(bool shouldHide, CodeMethod method, CodeClass parentClass, LanguageWriter writer)
    {
        if (shouldHide)
            writer.WriteLine("super.serialize(writer);");
        foreach (var otherProp in parentClass
                                        .GetPropertiesOfKind(CodePropertyKind.Custom, CodePropertyKind.ErrorMessageOverride)
                                        .Where(x => !x.ExistsInBaseType && !x.ReadOnly && !conventions.ErrorClassPropertyExistsInSuperClass(x))
                                        .OrderBy(static x => x.Name))
        {
            var serializationMethodName = GetSerializationMethodName(otherProp.Type, method);
            var booleanValue = serializationMethodName == "writeBoolValue" ? "value:" : "";
            var secondArgument = "";
            if (otherProp.Type is CodeType currentType && currentType.TypeDefinition is CodeEnum enumType)
            {
                secondArgument = $", (e) => e?.value";
            }
            writer.WriteLine($"writer.{serializationMethodName}('{otherProp.WireName}', {booleanValue}{otherProp.Name}{secondArgument});");
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
            var serializationMethodName = GetSerializationMethodName(otherProp.Type, method);
            var booleanValue = serializationMethodName == "writeBoolValue" ? "value:" : "";
            var secondArgument = "";
            if (otherProp.Type is CodeType currentType && currentType.TypeDefinition is CodeEnum enumType)
            {
                secondArgument = ", (e) => e?.value";
            }
            writer.StartBlock($"{(includeElse ? "else " : string.Empty)}if({otherProp.Name} != null) {{");
            writer.WriteLine($"writer.{serializationMethodName}(null, {booleanValue}{otherProp.Name}{secondArgument});");
            writer.CloseBlock();
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
            var serializationMethodName = GetSerializationMethodName(otherProp.Type, method);
            var secondArgument = "";
            if (otherProp.Type is CodeType currentType && currentType.TypeDefinition is CodeEnum enumType)
            {
                secondArgument = ", (e) => e?.value";
            }
            var booleanValue = serializationMethodName == "writeBoolValue" ? "value:" : "";
            writer.StartBlock($"{(includeElse ? "else " : string.Empty)}if({otherProp.Name} != null) {{");
            writer.WriteLine($"writer.{serializationMethodName}(null, {booleanValue}{otherProp.Name}{secondArgument});");
            writer.CloseBlock();
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
                writer.StartBlock("else {");
            }
            var firstPropertyName = complexProperties.First().Name;
            var propertiesNames = complexProperties.Skip(1).Any() ? complexProperties.Skip(1)
                                .Select(static x => x.Name)
                                .OrderBy(static x => x)
                                .Aggregate(static (x, y) => $"{x}, {y}") : string.Empty;
            var propertiesList = string.IsNullOrEmpty(propertiesNames) ? "" : $", [{propertiesNames}]";
            writer.WriteLine($"writer.{GetSerializationMethodName(complexProperties.First().Type, method)}(null, {firstPropertyName}{propertiesList});");
            if (includeElse)
            {
                writer.CloseBlock();
            }
        }
    }

    protected string GetSendRequestMethodName(bool isVoid, CodeElement currentElement, CodeTypeBase returnType)
    {
        ArgumentNullException.ThrowIfNull(returnType);
        var returnTypeName = conventions.GetTypeString(returnType, currentElement, false);
        var isStream = conventions.StreamTypeName.Equals(returnTypeName, StringComparison.OrdinalIgnoreCase);
        var isEnum = returnType is CodeType codeType && codeType.TypeDefinition is CodeEnum;
        if (isVoid) return "sendNoContent";
        else if (isStream || conventions.IsPrimitiveType(returnTypeName) || isEnum)
            if (returnType.IsCollection)
                return $"sendPrimitiveCollection<{returnTypeName.TrimEnd('?')}>";
            else
                return $"sendPrimitive<{returnTypeName}>";
        else if (returnType.IsCollection) return $"sendCollection<{returnTypeName}>";
        else if (returnType.Name.EqualsIgnoreCase("binary")) return "sendPrimitiveCollection<int>";
        else return $"send<{returnTypeName}>";
    }
    private void WriteMethodDocumentation(CodeMethod code, LanguageWriter writer)
    {
        conventions.WriteLongDescription(code, writer);
        foreach (var paramWithDescription in code.Parameters
                                                .Where(static x => x.Documentation.DescriptionAvailable)
                                                .OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase))
            conventions.WriteParameterDescription(paramWithDescription, writer);
        conventions.WriteDeprecationAttribute(code, writer);
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
                    thirdParameterName = $", {{RequestInformation.rawUrlKey : {rawUrlParameter.Name}}}";
                else if (parentClass.Properties.FirstOrDefaultOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProperty && !string.IsNullOrEmpty(pathParametersProperty.DefaultValue))
                    thirdParameterName = $", {pathParametersProperty.DefaultValue}";
                if (currentMethod.Parameters.OfKind(CodeParameterKind.RequestAdapter) is CodeParameter requestAdapterParameter)
                {
                    return $" : super({requestAdapterParameter.Name}, {urlTemplateProperty.DefaultValue}{thirdParameterName})";
                }
                else if (parentClass.StartBlock?.Inherits?.Name?.Contains("CliRequestBuilder", StringComparison.Ordinal) == true)
                {
                    // CLI uses a different base class.
                    return $" : super({urlTemplateProperty.DefaultValue}{thirdParameterName})";
                }
            }
            return " : super()";
        }
        else if (isConstructor && parentClass.Properties.Where(static x => x.IsOfKind(CodePropertyKind.AdditionalData)).Any() && !parentClass.IsErrorDefinition && !parentClass.Properties.Where(static x => x.IsOfKind(CodePropertyKind.BackingStore)).Any())
        {
            return " : ";
        }

        return string.Empty;
    }
    private void WriteMethodPrototype(CodeMethod code, CodeClass parentClass, LanguageWriter writer, string returnType, bool inherits, bool isVoid)
    {
        var staticModifier = code.IsStatic ? "static " : string.Empty;
        if (code.IsOfKind(CodeMethodKind.Serializer, CodeMethodKind.Deserializer, CodeMethodKind.QueryParametersMapper) || code.IsOfKind(CodeMethodKind.Custom))
        {
            writer.WriteLine("@override");
        }

        var genericTypeSuffix = code.IsAsync && !isVoid ? ">" : string.Empty;
        var isConstructor = code.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor, CodeMethodKind.RawUrlConstructor);
        var voidCorrectedTaskReturnType = code.IsAsync && isVoid ? "void" : returnType;
        var async = code.IsAsync ? " async" : string.Empty;
        if (code.ReturnType.IsArray && code.IsOfKind(CodeMethodKind.RequestExecutor))
            voidCorrectedTaskReturnType = $"IEnumerable<{voidCorrectedTaskReturnType.StripArraySuffix()}>";
        else if (code.IsOfKind(CodeMethodKind.RequestExecutor) && code.IsAsync)
            voidCorrectedTaskReturnType = $"Future<{voidCorrectedTaskReturnType.StripArraySuffix()}>";
        // TODO: Task type should be moved into the refiner
        var completeReturnType = isConstructor ?
            string.Empty : voidCorrectedTaskReturnType + " ";
        var baseSuffix = GetBaseSuffix(isConstructor, inherits, parentClass, code);
        var parameters = string.Join(", ", code.Parameters.OrderBy(static x => x, parameterOrderComparer).Select(p => conventions.GetParameterSignature(p, code)).ToList());
        var methodName = GetMethodName(code, parentClass, isConstructor);
        var includeNullableReferenceType = code.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator);
        var openingBracket = baseSuffix.Equals(" : ", StringComparison.Ordinal) ? "" : "{";
        var closingparenthesis = (isConstructor && parentClass.IsErrorDefinition) ? string.Empty : ")";
        // Constuctors (except for ClientConstructor) don't need a body but a closing statement
        if (HasEmptyConstructorBody(code, parentClass, isConstructor))
        {
            openingBracket = ";";
        }

        if (includeNullableReferenceType)
        {
            var completeReturnTypeWithNullable = isConstructor || string.IsNullOrEmpty(genericTypeSuffix) ? completeReturnType : $"{completeReturnType[..^2].TrimEnd('?')}?{genericTypeSuffix} ";
            var nullableParameters = string.Join(", ", code.Parameters.Order(parameterOrderComparer)
                                                          .Select(p => p.IsOfKind(CodeParameterKind.RequestConfiguration) ?
                                                                                        $"[{GetParameterSignatureWithNullableRefType(p, code)}]" :
                                                                                        conventions.GetParameterSignature(p, code))
                                                          .ToList());
            writer.WriteLine($"{staticModifier}{completeReturnTypeWithNullable}{conventions.GetAccessModifier(code.Access)}{methodName}({nullableParameters}){baseSuffix}{async} {{");
        }
        else if (parentClass.IsOfKind(CodeClassKind.Model) && code.IsOfKind(CodeMethodKind.Custom) && code.Name.EqualsIgnoreCase("copyWith"))
        {
            var parentParameters = "int? statusCode, String? message, Map<String, List<String>>? responseHeaders, Iterable<Object?>? innerExceptions, ";
            var ownParameters = string.Join(", ", parentClass.GetPropertiesOfKind(CodePropertyKind.Custom, CodePropertyKind.AdditionalData)
                                .Where(p => !conventions.ErrorClassPropertyExistsInSuperClass(p))
                                .Select(p => $"{GetPrefix(p)}{conventions.TranslateType(p.Type)}{getSuffix(p)}? {p.Name}"));
            writer.WriteLine($"{staticModifier}{completeReturnType}{conventions.GetAccessModifier(code.Access)}{methodName}({openingBracket}{parentParameters}{ownParameters} }}){{");
        }
        else
        {
            writer.WriteLine($"{staticModifier}{completeReturnType}{conventions.GetAccessModifier(code.Access)}{methodName}({parameters}{closingparenthesis}{baseSuffix}{async} {openingBracket}");
        }
    }

    private string getSuffix(CodeProperty property)
    {
        return property.Type.CollectionKind == CodeTypeCollectionKind.Complex ? ">" : string.Empty;
    }

    private string GetPrefix(CodeProperty property)
    {
        return property.Type.CollectionKind == CodeTypeCollectionKind.Complex ? "Iterable<" : string.Empty;
    }

    private static string GetMethodName(CodeMethod code, CodeClass parentClass, bool isConstructor)
    {
        if (code.IsOfKind(CodeMethodKind.RawUrlConstructor))
        {
            return parentClass.Name + ".withUrl";
        }
        return isConstructor ? parentClass.Name : code.Name;
    }

    private string GetParameterSignatureWithNullableRefType(CodeParameter parameter, CodeElement targetElement)
    {
        var signatureSegments = conventions.GetParameterSignature(parameter, targetElement).Split(" ", StringSplitOptions.RemoveEmptyEntries);
        if (signatureSegments.Length > 1 && signatureSegments[1].StartsWith("Function", StringComparison.Ordinal))
            signatureSegments[1] += "?";
        return $"{string.Join(" ", signatureSegments)}";
    }


    private void WriteQueryparametersBody(CodeClass parentClass, LanguageWriter writer)
    {
        writer.StartBlock("return {");
        foreach (CodeProperty property in parentClass.Properties)
        {
            var key = property.IsNameEscaped ? property.SerializationName : property.Name;
            writer.WriteLine($"'{key}' : {property.Name},");
        }
        writer.CloseBlock("};");
    }

    private string GetSerializationMethodName(CodeTypeBase propType, CodeMethod method, bool includeNullableRef = false)
    {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyType = conventions.GetTypeString(propType, method, false);
        if (propType is CodeType currentType)
        {
            if (isCollection)
                if (currentType.TypeDefinition == null)
                    return $"writeCollectionOfPrimitiveValues<{propertyType}>";
                else if (currentType.TypeDefinition is CodeEnum)
                    return $"writeCollectionOfEnumValues<{propertyType.TrimEnd('?')}>";
                else
                    return $"writeCollectionOfObjectValues<{propertyType}>";
            else if (currentType.TypeDefinition is CodeEnum enumType)
                return $"writeEnumValue<{enumType.Name}>";

        }

        return propertyType switch
        {
            "byte[]" => "writeByteArrayValue",
            "String" => "writeStringValue",
            "Iterable<int>" => "writeCollectionOfPrimitiveValues<int>",
            "UuidValue" => "writeUuidValue",
            _ when conventions.IsPrimitiveType(propertyType) => $"write{propertyType.TrimEnd(DartConventionService.NullableMarker).ToFirstCharacterUpperCase()}Value",
            _ => $"writeObjectValue<{propertyType.ToFirstCharacterUpperCase()}{(includeNullableRef ? "?" : "")}>",
        };
    }
    private void WriteCustomMethodBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        if (codeElement.Name.Equals("clone", StringComparison.OrdinalIgnoreCase))
        {
            var constructor = parentClass.GetMethodsOffKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor).Where(static x => x.Parameters.Any()).FirstOrDefault();
            var argumentList = constructor?.Parameters.OrderBy(static x => x, new BaseCodeParameterOrderComparer())
            .Select(static x => x.Type.Parent is CodeParameter param && param.IsOfKind(CodeParameterKind.RequestAdapter, CodeParameterKind.PathParameters)
                    ? x.Name :
                    x.Optional ? "null" : x.DefaultValue)
            .Aggregate(static (x, y) => $"{x}, {y}");
            writer.WriteLine($"return {parentClass.Name}({argumentList});");
        }
        if (codeElement.Name.Equals("copyWith", StringComparison.Ordinal))
        {
            var hasBackingStore = parentClass.Properties.Where(static x => x.IsOfKind(CodePropertyKind.BackingStore)).Any();
            var resultName = hasBackingStore ? "result" : string.Empty;

            if (hasBackingStore)
            {
                writer.WriteLine($"var {resultName} = {parentClass.Name}(");
            }
            else
            {
                writer.WriteLine($"return {parentClass.Name}(");
            }
            foreach (string prop in DartConventionService.ErrorClassProperties)
            {
                writer.WriteLine($"{prop} : {prop} ?? this.{prop}, ");
            }
            if (hasBackingStore)
            {
                writer.WriteLine(");");
            }
            foreach (CodeProperty prop in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom, CodePropertyKind.AdditionalData))
            {
                var propertyname = prop.Name;
                var separator = hasBackingStore ? "=" : ":";
                var ending = hasBackingStore ? ";" : ",";
                var resultPropertyName = string.IsNullOrEmpty(resultName) ? propertyname : $"{resultName}.{propertyname}";
                if (!conventions.ErrorClassPropertyExistsInSuperClass(prop))
                {
                    writer.WriteLine($"{resultPropertyName} {separator} {propertyname} ?? this.{propertyname}{ending} ");
                }
            }
            if (hasBackingStore)
            {
                writer.WriteLine($"return {resultName}; ");
            }
            else
            {
                writer.WriteLine($");");
            }
        }
    }
}
